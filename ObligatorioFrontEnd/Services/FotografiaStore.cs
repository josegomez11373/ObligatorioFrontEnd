using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Almacén compartido de metadatos y archivos de fotografía. Es singleton porque el
/// endpoint HTTP <c>/fotos</c> corre fuera del circuito de Blazor y necesita ver los
/// mismos metadatos que registró el servicio <see cref="FotografiaService"/>.
/// </summary>
public sealed class FotografiaStore
{
    private readonly object sync = new();
    private readonly List<Fotografia> fotografias = [];
    private readonly string rootPath;

    public FotografiaStore(IWebHostEnvironment environment)
    {
        rootPath = Path.GetFullPath(environment.ContentRootPath);
    }

    /// <summary>Carpeta física fuera de wwwroot donde viven las imágenes.</summary>
    public string UploadFolder => Path.Combine(rootPath, "App_Data", "uploads");

    public IReadOnlyList<Fotografia> All()
    {
        lock (sync)
        {
            return fotografias.ToList();
        }
    }

    public Fotografia? Find(Guid id)
    {
        lock (sync)
        {
            return fotografias.SingleOrDefault(f => f.Id == id);
        }
    }

    public void Add(Fotografia foto)
    {
        lock (sync)
        {
            fotografias.Add(foto);
        }
    }

    /// <summary>Reemplaza los metadatos de una foto existente sin borrar el archivo.</summary>
    public bool Update(Guid id, Fotografia replacement)
    {
        lock (sync)
        {
            var index = fotografias.FindIndex(f => f.Id == id);
            if (index < 0) return false;

            fotografias[index] = replacement;
            return true;
        }
    }

    /// <summary>Escribe el archivo y devuelve su ruta relativa al contenido de la aplicación.</summary>
    public string Save(byte[] content, string extension)
    {
        Directory.CreateDirectory(UploadFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(UploadFolder, fileName);
        File.WriteAllBytes(fullPath, content);

        return Path.GetRelativePath(rootPath, fullPath);
    }

    /// <summary>
    /// Resuelve el archivo para el endpoint HTTP. La autorización se hace mediante el
    /// token de capacidad emitido al crear la foto, porque el endpoint corre fuera del
    /// circuito de Blazor donde vive la sesión simulada.
    /// </summary>
    public (byte[] Content, string ContentType, string FileName)? Read(Guid id, string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var foto = Find(id);
        if (foto is null || !foto.Visible || foto.RutaRelativa is null || foto.ContentType is null) return null;
        if (string.IsNullOrWhiteSpace(foto.AccessToken)) return null;

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(foto.AccessToken),
                Encoding.UTF8.GetBytes(token)))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(rootPath, foto.RutaRelativa));
        var allowedRoot = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return null;
        }

        return (File.ReadAllBytes(fullPath), foto.ContentType, foto.NombreArchivo);
    }
}
