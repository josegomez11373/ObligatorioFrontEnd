using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class PotreroService
{
    private readonly IUserScope userScope;
    private readonly EstablecimientoService establecimientoService;
    private readonly List<Potrero> potreros = new();

    public event Action? StateChanged;

    public PotreroService(EstablecimientoService establecimientoService, IUserScope userScope)
    {
        this.establecimientoService = establecimientoService;
        this.userScope = userScope;
        potreros.Add(new Potrero { Id = SeedIds.PotreroLaEsperanza, EstablecimientoId = SeedIds.LaEsperanza, Nombre = "Potrero de Establecimiento La Esperanza", SuperficieHa = 60.25, CreatedBy = "seed" });
        potreros.Add(new Potrero { Id = SeedIds.PotreroElAlto, EstablecimientoId = SeedIds.ElAlto, Nombre = "Potrero de Establecimiento El Alto", SuperficieHa = 42.65, CreatedBy = "seed" });
    }

    public IReadOnlyList<Potrero> GetAll() => potreros
        .Where(p => establecimientoService.GetById(p.EstablecimientoId) is not null)
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
        EnsureEstablecimientoAccess(draft.EstablecimientoId);
        var p = draft with
        {
            Id = draft.Id == Guid.Empty ? Guid.NewGuid() : draft.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userScope.CurrentUser?.Email,
            UpdatedAt = null,
            UpdatedBy = null,
            Version = 1
        };
        potreros.Add(p);
        StateChanged?.Invoke();
        return p;
    }

    public bool Update(Potrero updated, long expectedVersion = 0)
    {
        EnsureCanManage();
        var idx = potreros.FindIndex(p => p.Id == updated.Id);
        if (idx < 0) return false;
        var existing = potreros[idx];
        EnsureExistingAccess(existing);
        EnsureEstablecimientoAccess(updated.EstablecimientoId);
        if (updated.EstablecimientoId != existing.EstablecimientoId)
        {
            throw new InvalidOperationException("El establecimiento de un potrero no se puede cambiar.");
        }
        EnsureVersion(existing.Version, expectedVersion);
        potreros[idx] = existing with
        {
            Nombre = updated.Nombre,
            SuperficieHa = updated.SuperficieHa,
            Limites = updated.Limites,
            Activo = updated.Activo,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        EnsureCanManage();
        var idx = potreros.FindIndex(p => p.Id == id);
        if (idx < 0) return false;
        var existing = potreros[idx];
        EnsureExistingAccess(existing);
        potreros[idx] = existing with { Activo = false, UpdatedAt = DateTime.UtcNow, UpdatedBy = userScope.CurrentUser?.Email, Version = existing.Version + 1 };
        StateChanged?.Invoke();
        return true;
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

    private void EnsureEstablecimientoAccess(Guid establecimientoId)
    {
        if (establecimientoService.GetById(establecimientoId) is null)
        {
            throw new InvalidOperationException("No tiene acceso al establecimiento seleccionado.");
        }
    }

    private void EnsureCanManage()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanManage(role, SigaModule.Potreros))
        {
            throw new InvalidOperationException("No tiene permisos para gestionar potreros.");
        }
    }
}
