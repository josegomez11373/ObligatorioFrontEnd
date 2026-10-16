using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class EmpresaService
{
    private readonly List<Empresa> empresas = new()
    {
        new Empresa { Nombre = "Agro del Sur" },
        new Empresa { Nombre = "Campos Verdes" },
    };

    public event Action? StateChanged;

    public IReadOnlyList<Empresa> GetAll() => empresas.OrderBy(e => e.Nombre).ToList();

    public Empresa? GetById(Guid id) => empresas.SingleOrDefault(e => e.Id == id);

    public Empresa Create(string nombre)
    {
        var emp = new Empresa { Nombre = nombre };
        empresas.Add(emp);
        StateChanged?.Invoke();
        return emp;
    }

    public bool Update(Empresa updated)
    {
        var idx = empresas.FindIndex(e => e.Id == updated.Id);
        if (idx < 0) return false;
        empresas[idx] = updated;
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        var idx = empresas.FindIndex(e => e.Id == id);
        if (idx < 0) return false;
        var existing = empresas[idx];
        empresas[idx] = existing with { Activa = false };
        StateChanged?.Invoke();
        return true;
    }
}
