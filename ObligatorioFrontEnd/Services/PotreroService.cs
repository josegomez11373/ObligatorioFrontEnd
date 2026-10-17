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
        // Semilla simple: crear un potrero por cada establecimiento existente
        foreach (var est in establecimientoService.GetAll())
        {
            potreros.Add(new Potrero { EstablecimientoId = est.Id, Nombre = $"Potrero de {est.Nombre}", SuperficieHa = Math.Round((est.SuperficieHa ?? 10) / 2, 2) });
        }
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

    public Potrero Create(Guid establecimientoId, string nombre, double? superficieHa)
    {
        EnsureCanManage();
        EnsureEstablecimientoAccess(establecimientoId);
        var p = new Potrero { EstablecimientoId = establecimientoId, Nombre = nombre, SuperficieHa = superficieHa };
        potreros.Add(p);
        StateChanged?.Invoke();
        return p;
    }

    public bool Update(Potrero updated)
    {
        EnsureCanManage();
        var idx = potreros.FindIndex(p => p.Id == updated.Id);
        if (idx < 0) return false;
        EnsureEstablecimientoAccess(updated.EstablecimientoId);
        var existing = potreros[idx];
        potreros[idx] = updated with { UpdatedAt = DateTime.UtcNow, Version = existing.Version + 1 };
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        EnsureCanManage();
        var idx = potreros.FindIndex(p => p.Id == id);
        if (idx < 0) return false;
        var existing = potreros[idx];
        EnsureEstablecimientoAccess(existing.EstablecimientoId);
        potreros[idx] = existing with { Activo = false, UpdatedAt = DateTime.UtcNow, Version = existing.Version + 1 };
        StateChanged?.Invoke();
        return true;
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
