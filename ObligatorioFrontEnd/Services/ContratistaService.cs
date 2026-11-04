using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class ContratistaService
{
    private readonly IUserScope userScope;
    private readonly EmpresaService empresaService;
    private readonly AuditService audit;
    private readonly List<Contratista> contratistas =
    [
        new() { EmpresaId = SeedIds.AgroDelSur, Nombre = "Servicios del Litoral", UsuarioId = SeedIds.UsuarioContratista },
        new() { EmpresaId = SeedIds.CamposVerdes, Nombre = "Cosechas del Norte" }
    ];

    public ContratistaService(IUserScope userScope, EmpresaService empresaService, AuditService audit)
    {
        this.userScope = userScope;
        this.empresaService = empresaService;
        this.audit = audit;
    }

    public event Action? StateChanged;

    public IReadOnlyList<Contratista> GetAll(bool includeInactive = false) => contratistas
        .Where(c => userScope.CanAccessEmpresa(c.EmpresaId) && (includeInactive || c.Activo))
        .OrderBy(c => c.Nombre)
        .ToList();

    public Contratista? GetById(Guid id) => contratistas.SingleOrDefault(c => c.Id == id && userScope.CanAccessEmpresa(c.EmpresaId));

    public Contratista? GetByUsuarioId(Guid usuarioId) =>
        contratistas.SingleOrDefault(c => c.UsuarioId == usuarioId && userScope.CanAccessEmpresa(c.EmpresaId));

    public Contratista Create(Contratista draft)
    {
        EnsureManage(draft.EmpresaId);
        Validate(draft, Guid.Empty);

        var item = draft with { Id = Guid.NewGuid(), Nombre = draft.Nombre.Trim() };
        contratistas.Add(item);
        audit.Record(nameof(Contratista), item.Id, "Crear", item.EmpresaId, null, item);
        StateChanged?.Invoke();
        return item;
    }

    public bool Update(Contratista updated)
    {
        var index = contratistas.FindIndex(c => c.Id == updated.Id);
        if (index < 0) return false;

        var current = contratistas[index];
        EnsureManage(current.EmpresaId);
        Validate(updated, current.Id);

        var replacement = current with
        {
            Nombre = updated.Nombre.Trim(),
            UsuarioId = updated.UsuarioId,
            Activo = updated.Activo
        };
        contratistas[index] = replacement;
        audit.Record(nameof(Contratista), current.Id, updated.Activo ? "Actualizar" : "Desactivar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        var index = contratistas.FindIndex(c => c.Id == id);
        if (index < 0) return false;

        var current = contratistas[index];
        EnsureManage(current.EmpresaId);
        if (!current.Activo) return true;

        var replacement = current with { Activo = false };
        contratistas[index] = replacement;
        audit.Record(nameof(Contratista), current.Id, "Desactivar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    private void Validate(Contratista item, Guid ignoredId)
    {
        if (string.IsNullOrWhiteSpace(item.Nombre))
        {
            throw new InvalidOperationException("El nombre del contratista es obligatorio.");
        }

        var name = item.Nombre.Trim();
        if (contratistas.Any(c => c.Id != ignoredId &&
                                  c.EmpresaId == item.EmpresaId &&
                                  c.Nombre.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un contratista con ese nombre.");
        }

        if (item.UsuarioId is Guid userId &&
            contratistas.Any(c => c.Id != ignoredId && c.UsuarioId == userId))
        {
            throw new InvalidOperationException("Ese usuario ya está vinculado a otro contratista.");
        }
    }

    private void EnsureManage(Guid empresaId)
    {
        var empresa = empresaService.GetById(empresaId);
        if (empresa is null || !empresa.Activo)
        {
            throw new InvalidOperationException("La empresa seleccionada no está disponible.");
        }

        if (!userScope.CanAccessEmpresa(empresaId) ||
            userScope.CurrentUser is not { Role: var role } ||
            !Permissions.CanManage(role, SigaModule.Catalogos))
        {
            throw new InvalidOperationException("No tiene permisos para gestionar contratistas.");
        }
    }
}
