using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class CultivoService : IServicioCultivos
{
    private readonly IUserScope userScope;
    private readonly EmpresaService empresaService;
    private readonly AuditService audit;
    private readonly NotificadorDatos notificador;

    private readonly List<Cultivo> cultivos =
    [
        new() { Id = SeedIds.SojaAgroDelSur, EmpresaId = SeedIds.AgroDelSur, Nombre = "Soja" },
        new() { Id = SeedIds.MaizAgroDelSur, EmpresaId = SeedIds.AgroDelSur, Nombre = "Maíz" },
        new() { Id = SeedIds.TrigoCamposVerdes, EmpresaId = SeedIds.CamposVerdes, Nombre = "Trigo" }
    ];

    public CultivoService(
        IUserScope userScope,
        EmpresaService empresaService,
        AuditService audit,
        NotificadorDatos notificador)
    {
        this.userScope = userScope;
        this.empresaService = empresaService;
        this.audit = audit;
        this.notificador = notificador;
    }

    public event Action? StateChanged;

    public IReadOnlyList<Cultivo> GetAll(bool includeInactive = false) => cultivos
        .Where(c => userScope.CanAccessEmpresa(c.EmpresaId) && (includeInactive || c.Activo))
        .OrderBy(c => c.Nombre)
        .ToList();

    public Cultivo? GetById(Guid id) => cultivos.SingleOrDefault(c => c.Id == id && userScope.CanAccessEmpresa(c.EmpresaId));

    public Cultivo Create(Guid empresaId, string nombre)
    {
        EnsureCanManage(empresaId);
        ValidateNombre(empresaId, nombre, Guid.Empty);

        var cultivo = new Cultivo { EmpresaId = empresaId, Nombre = nombre.Trim() };
        cultivos.Add(cultivo);
        audit.Record(nameof(Cultivo), cultivo.Id, "Crear", empresaId, null, cultivo);
        Notify();
        return cultivo;
    }

    public bool Update(Guid id, string nombre, bool activo)
    {
        var index = cultivos.FindIndex(c => c.Id == id);
        if (index < 0) return false;

        var current = cultivos[index];
        EnsureCanManage(current.EmpresaId);
        ValidateNombre(current.EmpresaId, nombre, current.Id);

        var replacement = current with { Nombre = nombre.Trim(), Activo = activo };
        cultivos[index] = replacement;
        audit.Record(nameof(Cultivo), current.Id, activo ? "Actualizar" : "Desactivar", current.EmpresaId, current, replacement);
        Notify();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        var cultivo = cultivos.SingleOrDefault(c => c.Id == id);
        return cultivo is not null && Update(id, cultivo.Nombre, false);
    }

    public Task<IReadOnlyList<CultivoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CultivoDto> result = GetAll(includeInactive: incluirInactivos)
            .Select(cultivo => cultivo.AContrato())
            .ToList();

        return Task.FromResult(result);
    }

    public Task<CultivoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cultivo = GetById(id);
        return Task.FromResult(cultivo?.AContrato());
    }

    public Task<CultivoDto> CreateAsync(
        CrearCultivoDto dto,
        CancellationToken cancellationToken = default)
    {
        var empresaId = dto.EmpresaId
            ?? userScope.CurrentUser?.EmpresaId
            ?? throw new InvalidOperationException("Debe indicar la empresa del cultivo.");

        return Task.FromResult(Create(empresaId, dto.Nombre).AContrato());
    }

    public Task<CultivoDto?> UpdateAsync(
        ActualizarCultivoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!Update(dto.Id, dto.Nombre, dto.Activo))
        {
            return Task.FromResult<CultivoDto?>(null);
        }

        return Task.FromResult<CultivoDto?>(cultivos.Single(c => c.Id == dto.Id).AContrato());
    }

    public Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Deactivate(id));

    private void Notify()
    {
        StateChanged?.Invoke();
        notificador.Notificar(ModulosDatos.Cultivos);
    }

    private void ValidateNombre(Guid empresaId, string nombre, Guid ignoredId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre del cultivo es obligatorio.");
        }

        var trimmed = nombre.Trim();
        if (cultivos.Any(c => c.Id != ignoredId &&
                              c.EmpresaId == empresaId &&
                              c.Nombre.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un cultivo con ese nombre.");
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
            !Permissions.CanManageCultivos(role))
        {
            throw new InvalidOperationException("No tiene permisos para gestionar cultivos.");
        }
    }
}
