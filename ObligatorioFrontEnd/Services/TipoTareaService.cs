using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class TipoTareaService
{
    private readonly IUserScope userScope;
    private readonly EmpresaService empresaService;
    private readonly AuditService audit;
    private readonly List<TipoTarea> tipos =
    [
        new() { Id = SeedIds.FumigacionAgroDelSur, EmpresaId = SeedIds.AgroDelSur, Nombre = "Fumigación" },
        new() { Id = SeedIds.FertilizacionAgroDelSur, EmpresaId = SeedIds.AgroDelSur, Nombre = "Fertilización" },
        new() { Id = SeedIds.CosechaCamposVerdes, EmpresaId = SeedIds.CamposVerdes, Nombre = "Cosecha" }
    ];

    public TipoTareaService(IUserScope userScope, EmpresaService empresaService, AuditService audit)
    {
        this.userScope = userScope;
        this.empresaService = empresaService;
        this.audit = audit;
    }

    public event Action? StateChanged;

    public IReadOnlyList<TipoTarea> GetAll(bool includeInactive = false) => tipos
        .Where(t => userScope.CanAccessEmpresa(t.EmpresaId) && (includeInactive || t.Activo))
        .OrderBy(t => t.Nombre)
        .ToList();

    public TipoTarea? GetById(Guid id) => tipos.SingleOrDefault(t => t.Id == id && userScope.CanAccessEmpresa(t.EmpresaId));

    public TipoTarea Create(Guid empresaId, string nombre)
    {
        EnsureCanManage(empresaId);
        ValidateNombre(empresaId, nombre, Guid.Empty);

        var tipo = new TipoTarea { EmpresaId = empresaId, Nombre = nombre.Trim() };
        tipos.Add(tipo);
        audit.Record(nameof(TipoTarea), tipo.Id, "Crear", empresaId, null, tipo);
        StateChanged?.Invoke();
        return tipo;
    }

    public bool Update(Guid id, string nombre, bool activo)
    {
        var index = tipos.FindIndex(t => t.Id == id);
        if (index < 0) return false;

        var current = tipos[index];
        EnsureCanManage(current.EmpresaId);
        ValidateNombre(current.EmpresaId, nombre, current.Id);

        var replacement = current with { Nombre = nombre.Trim(), Activo = activo };
        tipos[index] = replacement;
        audit.Record(nameof(TipoTarea), current.Id, activo ? "Actualizar" : "Desactivar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        var current = tipos.SingleOrDefault(t => t.Id == id);
        return current is not null && Update(id, current.Nombre, false);
    }

    private void ValidateNombre(Guid empresaId, string nombre, Guid ignoredId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre del tipo de tarea es obligatorio.");
        }

        var trimmed = nombre.Trim();
        if (tipos.Any(t => t.Id != ignoredId &&
                           t.EmpresaId == empresaId &&
                           t.Nombre.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un tipo de tarea con ese nombre.");
        }
    }

    private void EnsureCanManage(Guid empresaId)
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
            throw new InvalidOperationException("No tiene permisos para gestionar tipos de tarea.");
        }
    }
}
