using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class MaquinariaService
{
    private readonly IUserScope userScope;
    private readonly EmpresaService empresaService;
    private readonly AuditService audit;
    private readonly List<Maquinaria> maquinarias =
    [
        new() { EmpresaId = SeedIds.AgroDelSur, Nombre = "Tractor principal", CostoHora = 45m },
        new() { EmpresaId = SeedIds.CamposVerdes, Nombre = "Cosechadora", CostoHora = 80m }
    ];

    public MaquinariaService(IUserScope userScope, EmpresaService empresaService, AuditService audit)
    {
        this.userScope = userScope;
        this.empresaService = empresaService;
        this.audit = audit;
    }

    public event Action? StateChanged;

    public IReadOnlyList<Maquinaria> GetAll(bool includeInactive = false) => maquinarias
        .Where(m => userScope.CanAccessEmpresa(m.EmpresaId) && (includeInactive || m.Activo))
        .OrderBy(m => m.Nombre)
        .ToList();

    public Maquinaria? GetById(Guid id) => maquinarias.SingleOrDefault(m => m.Id == id && userScope.CanAccessEmpresa(m.EmpresaId));

    public Maquinaria Create(Maquinaria draft)
    {
        EnsureManage(draft.EmpresaId);
        Validate(draft, Guid.Empty);

        var item = draft with { Id = Guid.NewGuid(), Nombre = draft.Nombre.Trim() };
        maquinarias.Add(item);
        audit.Record(nameof(Maquinaria), item.Id, "Crear", item.EmpresaId, null, item);
        StateChanged?.Invoke();
        return item;
    }

    public bool Update(Maquinaria updated)
    {
        var index = maquinarias.FindIndex(m => m.Id == updated.Id);
        if (index < 0) return false;

        var current = maquinarias[index];
        EnsureManage(current.EmpresaId);
        Validate(updated, current.Id);

        var replacement = current with
        {
            Nombre = updated.Nombre.Trim(),
            CostoHora = updated.CostoHora,
            Activo = updated.Activo
        };
        maquinarias[index] = replacement;
        audit.Record(nameof(Maquinaria), current.Id, updated.Activo ? "Actualizar" : "Desactivar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        var index = maquinarias.FindIndex(m => m.Id == id);
        if (index < 0) return false;

        var current = maquinarias[index];
        EnsureManage(current.EmpresaId);
        if (!current.Activo) return true;

        var replacement = current with { Activo = false };
        maquinarias[index] = replacement;
        audit.Record(nameof(Maquinaria), current.Id, "Desactivar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    private void Validate(Maquinaria item, Guid ignoredId)
    {
        if (string.IsNullOrWhiteSpace(item.Nombre))
        {
            throw new InvalidOperationException("El nombre de la maquinaria es obligatorio.");
        }

        if (item.CostoHora < 0)
        {
            throw new InvalidOperationException("El costo por hora no puede ser negativo.");
        }

        var name = item.Nombre.Trim();
        if (maquinarias.Any(m => m.Id != ignoredId &&
                                 m.EmpresaId == item.EmpresaId &&
                                 m.Nombre.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe una maquinaria con ese nombre.");
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
            throw new InvalidOperationException("No tiene permisos para gestionar maquinaria.");
        }
    }
}
