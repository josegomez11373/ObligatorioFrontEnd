using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class EstablecimientoService : IServicioEstablecimientos
{
    private readonly IUserScope userScope;
    private readonly EmpresaService empresaService;
    private readonly AuditService audit;
    private readonly NotificadorDatos notificador;
    private readonly List<Establecimiento> establecimientos = new();

    public event Action? StateChanged;

    public EstablecimientoService(
        EmpresaService empresaService,
        IUserScope userScope,
        AuditService audit,
        NotificadorDatos notificador)
    {
        this.empresaService = empresaService;
        this.userScope = userScope;
        this.audit = audit;
        this.notificador = notificador;

        establecimientos.Add(new Establecimiento { Id = SeedIds.LaEsperanza, EmpresaId = SeedIds.AgroDelSur, Nombre = "Establecimiento La Esperanza", SuperficieHa = 120.5, CreatedBy = "seed" });
        establecimientos.Add(new Establecimiento { Id = SeedIds.ElAlto, EmpresaId = SeedIds.CamposVerdes, Nombre = "Establecimiento El Alto", SuperficieHa = 85.3, CreatedBy = "seed" });
    }

    public IReadOnlyList<Establecimiento> GetAll() => establecimientos
        .Where(CanAccess)
        .OrderBy(e => e.Nombre)
        .ToList();

    public Establecimiento? GetById(Guid id)
    {
        var establecimiento = establecimientos.SingleOrDefault(e => e.Id == id);
        return establecimiento is not null && CanAccess(establecimiento) ? establecimiento : null;
    }

    public Establecimiento Create(Establecimiento draft)
    {
        EnsureCanManage();
        EnsureEmpresaAccess(draft.EmpresaId, requireActive: true);
        ValidateNombre(draft.Nombre, draft.EmpresaId, Guid.Empty);

        var ent = draft with
        {
            Id = draft.Id == Guid.Empty ? Guid.NewGuid() : draft.Id,
            Nombre = draft.Nombre.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userScope.CurrentUser?.Email,
            UpdatedAt = null,
            UpdatedBy = null,
            Version = 1
        };
        establecimientos.Add(ent);
        audit.Record(nameof(Establecimiento), ent.Id, "Crear", ent.EmpresaId, null, ent);
        Notify();
        return ent;
    }

    public bool Update(Establecimiento updated, long expectedVersion = 0)
    {
        EnsureCanManage();
        var idx = establecimientos.FindIndex(e => e.Id == updated.Id);
        if (idx < 0) return false;

        var existing = establecimientos[idx];
        EnsureExistingAccess(existing);
        if (updated.EmpresaId != existing.EmpresaId)
        {
            throw new InvalidOperationException("La empresa de un establecimiento no se puede cambiar.");
        }

        EnsureEmpresaAccess(updated.EmpresaId, requireActive: updated.Activo);
        EnsureVersion(existing.Version, expectedVersion);
        ValidateNombre(updated.Nombre, existing.EmpresaId, existing.Id);

        var replacement = existing with
        {
            Nombre = updated.Nombre.Trim(),
            SuperficieHa = updated.SuperficieHa,
            Ubicacion = updated.Ubicacion?.Trim(),
            Responsables = updated.Responsables?.Trim(),
            Activo = updated.Activo,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        establecimientos[idx] = replacement;
        audit.Record(nameof(Establecimiento), existing.Id, updated.Activo ? "Actualizar" : "Desactivar", existing.EmpresaId, existing, replacement);
        Notify();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        EnsureCanManage();
        var idx = establecimientos.FindIndex(e => e.Id == id);
        if (idx < 0) return false;

        var existing = establecimientos[idx];
        EnsureExistingAccess(existing);
        if (!existing.Activo) return true;

        var replacement = existing with
        {
            Activo = false,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        establecimientos[idx] = replacement;
        audit.Record(nameof(Establecimiento), existing.Id, "Desactivar", existing.EmpresaId, existing, replacement);
        Notify();
        return true;
    }

    public Task<IReadOnlyList<EstablecimientoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EstablecimientoDto> result = establecimientos
            .Where(CanAccess)
            .Where(establecimiento => incluirInactivos || establecimiento.Activo)
            .OrderBy(establecimiento => establecimiento.Nombre)
            .Select(ADto)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<EstablecimientoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var establecimiento = GetById(id);
        return Task.FromResult(establecimiento is null ? null : ADto(establecimiento));
    }

    public Task<EstablecimientoDto> CreateAsync(
        CrearEstablecimientoDto dto,
        CancellationToken cancellationToken = default)
    {
        var empresaId = dto.EmpresaId
            ?? userScope.CurrentUser?.EmpresaId
            ?? throw new InvalidOperationException("Debe indicar la empresa del establecimiento.");

        var creado = Create(new Establecimiento
        {
            EmpresaId = empresaId,
            Nombre = dto.Nombre,
            Ubicacion = dto.Ubicacion,
            Responsables = dto.Responsables,
            SuperficieHa = dto.SuperficieHa
        });

        return Task.FromResult(ADto(creado));
    }

    public Task<EstablecimientoDto?> UpdateAsync(
        ActualizarEstablecimientoDto dto,
        CancellationToken cancellationToken = default)
    {
        var existing = establecimientos.SingleOrDefault(establecimiento => establecimiento.Id == dto.Id);
        if (existing is null)
        {
            return Task.FromResult<EstablecimientoDto?>(null);
        }

        var actualizado = Update(new Establecimiento
        {
            Id = dto.Id,
            EmpresaId = existing.EmpresaId,
            Nombre = dto.Nombre,
            Ubicacion = dto.Ubicacion,
            Responsables = dto.Responsables,
            SuperficieHa = dto.SuperficieHa,
            Activo = dto.Activo
        }, dto.Version);

        return Task.FromResult(actualizado ? ADto(establecimientos.Single(e => e.Id == dto.Id)) : null);
    }

    public Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Deactivate(id));

    private EstablecimientoDto ADto(Establecimiento establecimiento) =>
        establecimiento.AContrato(empresaService.GetById(establecimiento.EmpresaId)?.Nombre ?? "—");

    private void Notify()
    {
        StateChanged?.Invoke();
        notificador.Notificar(ModulosDatos.Establecimientos);
    }

    private void ValidateNombre(string nombre, Guid empresaId, Guid ignoredId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre del establecimiento es obligatorio.");
        }

        var trimmed = nombre.Trim();
        if (establecimientos.Any(e => e.Id != ignoredId && e.EmpresaId == empresaId && e.Nombre.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un establecimiento con ese nombre en la empresa.");
        }
    }

    private void EnsureExistingAccess(Establecimiento establecimiento)
    {
        if (!CanAccess(establecimiento))
        {
            throw new InvalidOperationException("No tiene acceso al establecimiento seleccionado.");
        }
    }

    private static void EnsureVersion(long currentVersion, long expectedVersion)
    {
        if (expectedVersion != 0 && currentVersion != expectedVersion)
        {
            throw new InvalidOperationException("El registro fue modificado por otro usuario. Recargue la información e intente nuevamente.");
        }
    }

    private bool CanAccess(Establecimiento establecimiento) =>
        userScope.CanAccessEmpresa(establecimiento.EmpresaId) &&
        userScope.CanAccessEstablecimiento(establecimiento.Id, establecimiento.EmpresaId);

    private void EnsureEmpresaAccess(Guid empresaId, bool requireActive)
    {
        var empresa = empresaService.GetById(empresaId);
        if (empresa is null)
        {
            throw new InvalidOperationException("No tiene acceso a la empresa seleccionada.");
        }

        if (requireActive && !empresa.Activo)
        {
            throw new InvalidOperationException("No se puede operar sobre una empresa desactivada.");
        }
    }

    private void EnsureCanManage()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanManage(role, SigaModule.Establecimientos))
        {
            throw new InvalidOperationException("No tiene permisos para gestionar establecimientos.");
        }
    }
}
