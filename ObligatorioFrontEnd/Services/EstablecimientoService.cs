using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class EstablecimientoService
{
    private readonly IUserScope userScope;
    private readonly EmpresaService empresaService;
    private readonly List<Establecimiento> establecimientos = new();

    public event Action? StateChanged;

    public EstablecimientoService(EmpresaService empresaService, IUserScope userScope)
    {
        this.empresaService = empresaService;
        this.userScope = userScope;
        // Semilla de establecimientos asociados a las empresas existentes
        establecimientos.Add(new Establecimiento { Id = SeedIds.LaEsperanza, EmpresaId = SeedIds.AgroDelSur, Nombre = "Establecimiento La Esperanza", SuperficieHa = 120.5 });
        establecimientos.Add(new Establecimiento { Id = SeedIds.ElAlto, EmpresaId = SeedIds.CamposVerdes, Nombre = "Establecimiento El Alto", SuperficieHa = 85.3 });
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

    public Establecimiento Create(Guid empresaId, string nombre, double? superficieHa)
    {
        EnsureCanManage();
        EnsureEmpresaAccess(empresaId);
        var ent = new Establecimiento { EmpresaId = empresaId, Nombre = nombre, SuperficieHa = superficieHa };
        establecimientos.Add(ent);
        StateChanged?.Invoke();
        return ent;
    }

    public bool Update(Establecimiento updated)
    {
        EnsureCanManage();
        var idx = establecimientos.FindIndex(e => e.Id == updated.Id);
        if (idx < 0) return false;
        EnsureEmpresaAccess(updated.EmpresaId);
        var existing = establecimientos[idx];
        establecimientos[idx] = updated with { UpdatedAt = DateTime.UtcNow, Version = existing.Version + 1 };
        StateChanged?.Invoke();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        EnsureCanManage();
        var idx = establecimientos.FindIndex(e => e.Id == id);
        if (idx < 0) return false;
        var existing = establecimientos[idx];
        EnsureEmpresaAccess(existing.EmpresaId);
        establecimientos[idx] = existing with { Activo = false, UpdatedAt = DateTime.UtcNow, Version = existing.Version + 1 };
        StateChanged?.Invoke();
        return true;
    }

    private bool CanAccess(Establecimiento establecimiento) =>
        userScope.CanAccessEmpresa(establecimiento.EmpresaId) &&
        userScope.CanAccessEstablecimiento(establecimiento.Id);

    private void EnsureEmpresaAccess(Guid empresaId)
    {
        if (!userScope.CanAccessEmpresa(empresaId))
        {
            throw new InvalidOperationException("No tiene acceso a la empresa seleccionada.");
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
