using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class PotreroService : IServicioPotreros
{
    private readonly IUserScope userScope;
    private readonly EstablecimientoService establecimientoService;
    private readonly AuditService audit;
    private readonly NotificadorDatos notificador;
    private readonly List<Potrero> potreros = new();

    public event Action? StateChanged;

    public PotreroService(
        EstablecimientoService establecimientoService,
        IUserScope userScope,
        AuditService audit,
        NotificadorDatos notificador)
    {
        this.establecimientoService = establecimientoService;
        this.userScope = userScope;
        this.audit = audit;
        this.notificador = notificador;
        potreros.Add(new Potrero { Id = SeedIds.PotreroLaEsperanza, EstablecimientoId = SeedIds.LaEsperanza, Nombre = "Potrero de Establecimiento La Esperanza", SuperficieHa = 60.25, CreatedBy = "seed" });
        potreros.Add(new Potrero { Id = SeedIds.PotreroElAlto, EstablecimientoId = SeedIds.ElAlto, Nombre = "Potrero de Establecimiento El Alto", SuperficieHa = 42.65, CreatedBy = "seed" });
    }

    public IReadOnlyList<Potrero> GetAll(bool includeInactive = false) => potreros
        .Where(p => (includeInactive || p.Activo) && establecimientoService.GetById(p.EstablecimientoId) is not null)
        .OrderBy(p => p.Nombre)
        .ToList();

    public IReadOnlyList<Potrero> GetByEstablecimiento(Guid establecimientoId, bool includeInactive = false) => potreros
        .Where(p => p.EstablecimientoId == establecimientoId &&
                    (includeInactive || p.Activo) &&
                    establecimientoService.GetById(p.EstablecimientoId) is not null)
        .OrderBy(p => p.Nombre)
        .ToList();

    public Potrero? GetById(Guid id)
    {
        var potrero = potreros.SingleOrDefault(p => p.Id == id);
        return potrero is not null && establecimientoService.GetById(potrero.EstablecimientoId) is not null ? potrero : null;
    }

    public Potrero Create(Potrero draft)
    {
        EnsureCanManage();
        EnsureEstablecimientoAccess(draft.EstablecimientoId, requireActive: true);
        ValidateNombre(draft.Nombre, draft.EstablecimientoId, Guid.Empty);

        var p = draft with
        {
            Id = draft.Id == Guid.Empty ? Guid.NewGuid() : draft.Id,
            Nombre = draft.Nombre.Trim(),
            Limites = draft.Limites?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userScope.CurrentUser?.Email,
            UpdatedAt = null,
            UpdatedBy = null,
            Version = 1
        };
        potreros.Add(p);
        audit.Record(nameof(Potrero), p.Id, "Crear", EmpresaDe(p), null, p);
        Notify();
        return p;
    }

    public bool Update(Potrero updated, long expectedVersion = 0)
    {
        EnsureCanManage();
        var idx = potreros.FindIndex(p => p.Id == updated.Id);
        if (idx < 0) return false;

        var existing = potreros[idx];
        EnsureExistingAccess(existing);
        if (updated.EstablecimientoId != existing.EstablecimientoId)
        {
            throw new InvalidOperationException("El establecimiento de un potrero no se puede cambiar.");
        }

        EnsureEstablecimientoAccess(updated.EstablecimientoId, requireActive: updated.Activo);
        EnsureVersion(existing.Version, expectedVersion);
        ValidateNombre(updated.Nombre, existing.EstablecimientoId, existing.Id);

        var replacement = existing with
        {
            Nombre = updated.Nombre.Trim(),
            SuperficieHa = updated.SuperficieHa,
            Limites = updated.Limites?.Trim(),
            Activo = updated.Activo,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        potreros[idx] = replacement;
        audit.Record(nameof(Potrero), existing.Id, updated.Activo ? "Actualizar" : "Desactivar", EmpresaDe(existing), existing, replacement);
        Notify();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        EnsureCanManage();
        var idx = potreros.FindIndex(p => p.Id == id);
        if (idx < 0) return false;

        var existing = potreros[idx];
        EnsureExistingAccess(existing);
        if (!existing.Activo) return true;

        var replacement = existing with
        {
            Activo = false,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        potreros[idx] = replacement;
        audit.Record(nameof(Potrero), existing.Id, "Desactivar", EmpresaDe(existing), existing, replacement);
        Notify();
        return true;
    }

    public Task<IReadOnlyList<PotreroDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PotreroDto> result = GetAll(includeInactive: incluirInactivos)
            .Select(ADto)
            .Where(dto => dto is not null)
            .Cast<PotreroDto>()
            .ToList();

        return Task.FromResult(result);
    }

    public Task<PotreroDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var potrero = GetById(id);
        return Task.FromResult(potrero is null ? null : ADto(potrero));
    }

    public Task<PotreroDto> CreateAsync(
        CrearPotreroDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = Create(new Potrero
        {
            EstablecimientoId = dto.EstablecimientoId,
            Nombre = dto.Nombre,
            SuperficieHa = dto.SuperficieHa,
            Limites = dto.Limites
        });

        return Task.FromResult(ADto(creado)!);
    }

    public Task<PotreroDto?> UpdateAsync(
        ActualizarPotreroDto dto,
        CancellationToken cancellationToken = default)
    {
        var actualizado = Update(new Potrero
        {
            Id = dto.Id,
            EstablecimientoId = dto.EstablecimientoId,
            Nombre = dto.Nombre,
            SuperficieHa = dto.SuperficieHa,
            Limites = dto.Limites,
            Activo = dto.Activo
        }, dto.Version);

        return Task.FromResult(actualizado ? ADto(potreros.Single(p => p.Id == dto.Id)) : null);
    }

    public Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Deactivate(id));

    private PotreroDto? ADto(Potrero potrero)
    {
        var establecimiento = establecimientoService.GetById(potrero.EstablecimientoId);
        return establecimiento is null ? null : potrero.AContrato(establecimiento);
    }

    private void Notify()
    {
        StateChanged?.Invoke();
        notificador.Notificar(ModulosDatos.Potreros);
    }

    private void ValidateNombre(string nombre, Guid establecimientoId, Guid ignoredId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre del potrero es obligatorio.");
        }

        var trimmed = nombre.Trim();
        if (potreros.Any(p => p.Id != ignoredId &&
                              p.EstablecimientoId == establecimientoId &&
                              p.Nombre.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe un potrero con ese nombre en el establecimiento.");
        }
    }

    private void EnsureExistingAccess(Potrero potrero)
    {
        if (establecimientoService.GetById(potrero.EstablecimientoId) is null)
        {
            throw new InvalidOperationException("No tiene acceso al potrero seleccionado.");
        }
    }

    private static void EnsureVersion(long currentVersion, long expectedVersion)
    {
        if (expectedVersion != 0 && currentVersion != expectedVersion)
        {
            throw new InvalidOperationException("El registro fue modificado por otro usuario. Recargue la información e intente nuevamente.");
        }
    }

    private void EnsureEstablecimientoAccess(Guid establecimientoId, bool requireActive)
    {
        var establecimiento = establecimientoService.GetById(establecimientoId);
        if (establecimiento is null)
        {
            throw new InvalidOperationException("No tiene acceso al establecimiento seleccionado.");
        }

        if (requireActive && !establecimiento.Activo)
        {
            throw new InvalidOperationException("No se puede operar sobre un establecimiento desactivado.");
        }
    }

    private Guid? EmpresaDe(Potrero potrero) => establecimientoService.GetById(potrero.EstablecimientoId)?.EmpresaId;

    private void EnsureCanManage()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanManage(role, SigaModule.Potreros))
        {
            throw new InvalidOperationException("No tiene permisos para gestionar potreros.");
        }
    }
}
