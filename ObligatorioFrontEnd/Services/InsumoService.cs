using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class InsumoService
{
    private readonly IUserScope userScope;
    private readonly EmpresaService empresaService;
    private readonly AuditService audit;
    private readonly List<Insumo> insumos =
    [
        new() { EmpresaId = SeedIds.AgroDelSur, Nombre = "Fertilizante NPK", Unidad = "kg", PrecioUnitario = 2.50m },
        new() { EmpresaId = SeedIds.CamposVerdes, Nombre = "Semilla de trigo", Unidad = "kg", PrecioUnitario = 1.80m }
    ];

    public InsumoService(IUserScope userScope, EmpresaService empresaService, AuditService audit)
    {
        this.userScope = userScope;
        this.empresaService = empresaService;
        this.audit = audit;
    }

    public event Action? StateChanged;

    public IReadOnlyList<Insumo> GetAll(bool includeInactive = false) => insumos
        .Where(i => userScope.CanAccessEmpresa(i.EmpresaId) && (includeInactive || i.Activo))
        .OrderBy(i => i.Nombre)
        .ToList();

    public Insumo? GetById(Guid id) => insumos.SingleOrDefault(i => i.Id == id && userScope.CanAccessEmpresa(i.EmpresaId));

    public Insumo Create(Insumo draft)
    {
        EnsureManage(draft.EmpresaId);
        Validate(draft, Guid.Empty);

        var item = draft with
        {
            Id = Guid.NewGuid(),
            Nombre = draft.Nombre.Trim(),
            Unidad = draft.Unidad?.Trim()
        };
        insumos.Add(item);
        audit.Record(nameof(Insumo), item.Id, "Crear", item.EmpresaId, null, item);
        StateChanged?.Invoke();
        return item;
    }

    public bool Update(Insumo updated)
    {
        var index = insumos.FindIndex(i => i.Id == updated.Id);
        if (index < 0) return false;

        var current = insumos[index];
        EnsureManage(current.EmpresaId);
        Validate(updated, current.Id);

        var replacement = current with
        {
            Nombre = updated.Nombre.Trim(),
            Unidad = updated.Unidad?.Trim(),
            PrecioUnitario = updated.PrecioUnitario,
            Activo = updated.Activo
        };
        insumos[index] = replacement;
        audit.Record(nameof(Insumo), current.Id, updated.Activo ? "Actualizar" : "Desactivar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        var index = insumos.FindIndex(i => i.Id == id);
        if (index < 0) return false;

        var current = insumos[index];
        EnsureManage(current.EmpresaId);
        if (!current.Activo) return true;

        var replacement = current with { Activo = false };
        insumos[index] = replacement;
        audit.Record(nameof(Insumo), current.Id, "Desactivar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    private void Validate(Insumo item, Guid ignoredId)
    {
        if (string.IsNullOrWhiteSpace(item.Nombre))
        {
            throw new InvalidOperationException("El nombre del insumo es obligatorio.");
        }

        if (item.PrecioUnitario < 0)
        {
            throw new InvalidOperationException("El precio del insumo no puede ser negativo.");
        }

        var name = item.Nombre.Trim();
        if (insumos.Any(i => i.Id != ignoredId &&
                             i.EmpresaId == item.EmpresaId &&
                             i.Nombre.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un insumo con ese nombre.");
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
            throw new InvalidOperationException("No tiene permisos para gestionar insumos.");
        }
    }
}
