using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class EstablecimientoService
{
    private readonly List<Establecimiento> establecimientos = new();

    public event Action? StateChanged;

    public EstablecimientoService(EmpresaService empresaService)
    {
        // Semilla de establecimientos asociados a las empresas existentes
        var empresas = empresaService.GetAll().ToList();
        if (empresas.Count >= 1)
        {
            establecimientos.Add(new Establecimiento { EmpresaId = empresas[0].Id, Nombre = "Establecimiento La Esperanza", SuperficieHa = 120.5 });
        }

        if (empresas.Count >= 2)
        {
            establecimientos.Add(new Establecimiento { EmpresaId = empresas[1].Id, Nombre = "Establecimiento El Alto", SuperficieHa = 85.3 });
        }
    }

    public IReadOnlyList<Establecimiento> GetAll() => establecimientos.OrderBy(e => e.Nombre).ToList();

    public Establecimiento? GetById(Guid id) => establecimientos.SingleOrDefault(e => e.Id == id);

    public Establecimiento Create(Guid empresaId, string nombre, double? superficieHa)
    {
        var ent = new Establecimiento { EmpresaId = empresaId, Nombre = nombre, SuperficieHa = superficieHa };
        establecimientos.Add(ent);
        StateChanged?.Invoke();
        return ent;
    }

    public bool Update(Establecimiento updated)
    {
        var idx = establecimientos.FindIndex(e => e.Id == updated.Id);
        if (idx < 0) return false;
        establecimientos[idx] = updated;
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        var idx = establecimientos.FindIndex(e => e.Id == id);
        if (idx < 0) return false;
        var existing = establecimientos[idx];
        establecimientos[idx] = existing with { Activo = false };
        StateChanged?.Invoke();
        return true;
    }
}
