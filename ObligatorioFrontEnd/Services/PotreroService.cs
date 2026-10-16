using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class PotreroService
{
    private readonly List<Potrero> potreros = new();

    public event Action? StateChanged;

    public PotreroService(EstablecimientoService establecimientoService)
    {
        // Semilla simple: crear un potrero por cada establecimiento existente
        foreach (var est in establecimientoService.GetAll())
        {
            potreros.Add(new Potrero { EstablecimientoId = est.Id, Nombre = $"Potrero de {est.Nombre}", SuperficieHa = Math.Round((est.SuperficieHa ?? 10) / 2, 2) });
        }
    }

    public IReadOnlyList<Potrero> GetAll() => potreros.OrderBy(p => p.Nombre).ToList();

    public Potrero? GetById(Guid id) => potreros.SingleOrDefault(p => p.Id == id);

    public Potrero Create(Guid establecimientoId, string nombre, double? superficieHa)
    {
        var p = new Potrero { EstablecimientoId = establecimientoId, Nombre = nombre, SuperficieHa = superficieHa };
        potreros.Add(p);
        StateChanged?.Invoke();
        return p;
    }

    public bool Update(Potrero updated)
    {
        var idx = potreros.FindIndex(p => p.Id == updated.Id);
        if (idx < 0) return false;
        potreros[idx] = updated;
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        var idx = potreros.FindIndex(p => p.Id == id);
        if (idx < 0) return false;
        var existing = potreros[idx];
        potreros[idx] = existing with { Activo = false };
        StateChanged?.Invoke();
        return true;
    }
}
