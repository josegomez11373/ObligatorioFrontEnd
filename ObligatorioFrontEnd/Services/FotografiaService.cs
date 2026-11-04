using System.Security.Cryptography;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class FotografiaService
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/heic"] = ".heic",
            ["image/heif"] = ".heic"
        };

    private static readonly string[] HeicBrands = ["heic", "heix", "hevc", "hevx", "mif1", "msf1"];

    private readonly IUserScope userScope;
    private readonly AuditService audit;
    private readonly FotografiaStore store;

    public FotografiaService(IUserScope userScope, AuditService audit, FotografiaStore store)
    {
        this.userScope = userScope;
        this.audit = audit;
        this.store = store;
    }

    public event Action? StateChanged;

    public IReadOnlyList<Fotografia> GetByEvento(Guid eventoId) => store.All()
        .Where(f => f.EventoId == eventoId && userScope.CanAccessEmpresa(f.EmpresaId))
        .OrderByDescending(f => f.FechaCaptura)
        .ToList();

    public IReadOnlyList<Fotografia> GetVisibleByEvento(Guid eventoId) => GetByEvento(eventoId)
        .Where(f => f.Visible)
        .ToList();

    public Fotografia? GetById(Guid id)
    {
        var foto = store.Find(id);
        return foto is not null && userScope.CanAccessEmpresa(foto.EmpresaId) ? foto : null;
    }

    public IReadOnlyList<Fotografia> GetAll() => store.All()
        .Where(f => userScope.CanAccessEmpresa(f.EmpresaId))
        .OrderByDescending(f => f.FechaCaptura)
        .ToList();

    /// <summary>
    /// Guarda las imágenes fuera de wwwroot y registra los metadatos. No se eliminan
    /// físicamente: una foto solo puede ocultarse con un motivo.
    /// </summary>
    public IReadOnlyList<Fotografia> CreateMany(
        Guid empresaId,
        Guid eventoId,
        IReadOnlyList<FotoUpload> fotos,
        double? latitud = null,
        double? longitud = null)
    {
        EnsureCanUpload(empresaId);
        if (fotos.Count == 0) return [];

        var created = new List<Fotografia>();

        foreach (var foto in fotos)
        {
            ValidateUpload(foto);
            var extension = AllowedTypes[foto.ContentType];
            var rutaRelativa = store.Save(foto.Contenido, extension);

            var item = new Fotografia
            {
                EmpresaId = empresaId,
                EventoId = eventoId,
                NombreArchivo = SafeName(foto.NombreArchivo, extension),
                RutaRelativa = rutaRelativa,
                AccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
                ContentType = foto.ContentType,
                TamanoBytes = foto.Contenido.LongLength,
                Autor = userScope.CurrentUser?.Name,
                FechaCaptura = SigaTime.UtcNow,
                Hash = Convert.ToHexString(SHA256.HashData(foto.Contenido)),
                Latitud = latitud,
                Longitud = longitud,
                CreatedAt = SigaTime.UtcNow,
                CreatedBy = userScope.CurrentUser?.Email,
                Version = 1
            };

            store.Add(item);
            audit.Record(nameof(Fotografia), item.Id, "Crear", empresaId, null, new
            {
                item.NombreArchivo,
                item.ContentType,
                item.TamanoBytes,
                item.Hash
            });
            created.Add(item);
        }

        StateChanged?.Invoke();
        return created;
    }

    public bool Ocultar(Guid id, string motivo)
    {
        EnsureCanHide();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new InvalidOperationException("El motivo de ocultamiento es obligatorio.");
        }

        var existing = store.Find(id);
        if (existing is null || !userScope.CanAccessEmpresa(existing.EmpresaId))
        {
            return false;
        }

        if (!existing.Visible) return true;

        var replacement = existing with
        {
            Visible = false,
            MotivoOcultamiento = motivo.Trim(),
            UpdatedAt = SigaTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };

        if (!store.Update(id, replacement)) return false;

        audit.Record(nameof(Fotografia), existing.Id, "Ocultar", existing.EmpresaId, existing, replacement);
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>URL con token de capacidad para mostrar la imagen desde el navegador.</summary>
    public string? GetUrl(Fotografia foto) => foto.Visible && !string.IsNullOrWhiteSpace(foto.AccessToken)
        ? $"/fotos/{foto.Id}?token={Uri.EscapeDataString(foto.AccessToken)}"
        : null;

    /// <summary>Delegado al almacén singleton que usa el endpoint HTTP.</summary>
    public (byte[] Content, string ContentType, string FileName)? GetFile(Guid id, string? token) =>
        store.Read(id, token);

    private static void ValidateUpload(FotoUpload foto)
    {
        if (foto.Contenido.Length == 0)
        {
            throw new InvalidOperationException($"El archivo {foto.NombreArchivo} está vacío.");
        }

        if (foto.Contenido.LongLength > MaxFileSize)
        {
            throw new InvalidOperationException($"El archivo {foto.NombreArchivo} supera el máximo de 10 MB.");
        }

        if (!AllowedTypes.ContainsKey(foto.ContentType))
        {
            throw new InvalidOperationException($"El formato de {foto.NombreArchivo} no está permitido. Use JPG, PNG o HEIC.");
        }

        if (!LooksLikeImage(foto.Contenido, foto.ContentType))
        {
            throw new InvalidOperationException($"El archivo {foto.NombreArchivo} no parece ser una imagen válida.");
        }
    }

    private static bool LooksLikeImage(byte[] content, string contentType)
    {
        if (content.Length < 12) return false;

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            "image/png" => content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47,
            "image/heic" or "image/heif" => IsHeic(content),
            _ => false
        };
    }

    /// <summary>HEIC/HEIF es un contenedor ISO BMFF: bytes 4-7 "ftyp" y marca en 8-11.</summary>
    private static bool IsHeic(byte[] content)
    {
        if (content[4] != (byte)'f' || content[5] != (byte)'t' || content[6] != (byte)'y' || content[7] != (byte)'p')
        {
            return false;
        }

        var brand = System.Text.Encoding.ASCII.GetString(content, 8, 4);
        return HeicBrands.Contains(brand, StringComparer.OrdinalIgnoreCase);
    }

    private static string SafeName(string original, string extension)
    {
        var name = Path.GetFileName(original);
        return string.IsNullOrWhiteSpace(name) ? $"foto{extension}" : name;
    }

    private void EnsureCanUpload(Guid empresaId)
    {
        if (!userScope.CanAccessEmpresa(empresaId))
        {
            throw new InvalidOperationException("No tiene acceso a la empresa seleccionada.");
        }

        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanManage(role, SigaModule.Eventos))
        {
            throw new InvalidOperationException("No tiene permisos para registrar fotografías.");
        }
    }

    private void EnsureCanHide()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanHidePhoto(role))
        {
            throw new InvalidOperationException("No tiene permisos para ocultar fotografías.");
        }
    }
}
