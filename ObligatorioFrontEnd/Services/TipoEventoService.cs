using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class TipoEventoService
{
    private readonly IUserScope userScope;
    private readonly EmpresaService empresaService;
    private readonly AuditService audit;
    private readonly List<TipoEvento> tipos =
    [
        new() { Id = SeedIds.PlagaAgroDelSur, EmpresaId = SeedIds.AgroDelSur, Nombre = "Plaga" },
        new() { Id = SeedIds.MalezaAgroDelSur, EmpresaId = SeedIds.AgroDelSur, Nombre = "Maleza" },
        new() { Id = SeedIds.LluviaAgroDelSur, EmpresaId = SeedIds.AgroDelSur, Nombre = "Lluvia intensa" },
        new() { Id = SeedIds.EnfermedadCamposVerdes, EmpresaId = SeedIds.CamposVerdes, Nombre = "Enfermedad" }
    ];

    public TipoEventoService(IUserScope userScope, EmpresaService empresaService, AuditService audit)
    {
        this.userScope = userScope;
        this.empresaService = empresaService;
        this.audit = audit;
    }

    public event Action? StateChanged;

    public IReadOnlyList<TipoEvento> GetAll(bool includeInactive = false) => tipos
        .Where(t => userScope.CanAccessEmpresa(t.EmpresaId) && (includeInactive || t.Activo))
        .OrderBy(t => t.Nombre)
        .ToList();

    public TipoEvento? GetById(Guid id) => tipos.SingleOrDefault(t => t.Id == id && userScope.CanAccessEmpresa(t.EmpresaId));

    public TipoEvento Create(Guid empresaId, string nombre)
    {
        EnsureCanManage(empresaId);
        ValidateNombre(empresaId, nombre, Guid.Empty);

        var tipo = new TipoEvento { EmpresaId = empresaId, Nombre = nombre.Trim() };
        tipos.Add(tipo);
        audit.Record(nameof(TipoEvento), tipo.Id, "Crear", empresaId, null, tipo);
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
        audit.Record(nameof(TipoEvento), current.Id, activo ? "Actualizar" : "Desactivar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id) => UpdateStatus(id, false);

    private bool UpdateStatus(Guid id, bool activo)
    {
        var index = tipos.FindIndex(t => t.Id == id);
        if (index < 0) return false;
        EnsureCanManage(tipos[index].EmpresaId);
        var current = tipos[index];
        var replacement = current with { Activo = activo };
        tipos[index] = replacement;
        audit.Record(nameof(TipoEvento), current.Id, activo ? "Activar" : "Desactivar", current.EmpresaId, current, replacement);
        StateChanged?.Invoke();
        return true;
    }

    private void ValidateNombre(Guid empresaId, string nombre, Guid ignoredId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre del tipo de evento es obligatorio.");
        }

        var trimmed = nombre.Trim();
        if (tipos.Any(t => t.Id != ignoredId &&
                           t.EmpresaId == empresaId &&
                           t.Nombre.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un tipo de evento con ese nombre.");
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
            throw new InvalidOperationException("No tiene permisos para gestionar tipos de evento.");
        }
    }
}
