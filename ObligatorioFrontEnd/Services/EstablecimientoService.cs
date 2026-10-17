using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class EstablecimientoService
{
    private readonly IUserScope userScope;
    private readonly List<Establecimiento> establecimientos = new();

    public event Action? StateChanged;

    public EstablecimientoService(EmpresaService empresaService, IUserScope userScope)
    {
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

    public Establecimiento Create(Establecimiento draft)
    {
        EnsureCanManage();
        EnsureEmpresaAccess(draft.EmpresaId);
        var ent = draft with
        {
            Id = draft.Id == Guid.Empty ? Guid.NewGuid() : draft.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userScope.CurrentUser?.Email,
            UpdatedAt = null,
            UpdatedBy = null,
            Version = 1
        };
        establecimientos.Add(ent);
        StateChanged?.Invoke();
        return ent;
    }

    public bool Update(Establecimiento updated, long expectedVersion = 0)
    {
        EnsureCanManage();
        var idx = establecimientos.FindIndex(e => e.Id == updated.Id);
        if (idx < 0) return false;
        var existing = establecimientos[idx];
        EnsureExistingAccess(existing);
        EnsureEmpresaAccess(updated.EmpresaId);
        if (updated.EmpresaId != existing.EmpresaId)
        {
            throw new InvalidOperationException("La empresa de un establecimiento no se puede cambiar.");
        }
        EnsureVersion(existing.Version, expectedVersion);
        establecimientos[idx] = existing with
        {
            Nombre = updated.Nombre,
            SuperficieHa = updated.SuperficieHa,
            Ubicacion = updated.Ubicacion,
            Responsables = updated.Responsables,
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
        var idx = establecimientos.FindIndex(e => e.Id == id);
        if (idx < 0) return false;
        var existing = establecimientos[idx];
        EnsureExistingAccess(existing);
        establecimientos[idx] = existing with { Activo = false, UpdatedAt = DateTime.UtcNow, UpdatedBy = userScope.CurrentUser?.Email, Version = existing.Version + 1 };
        StateChanged?.Invoke();
        return true;
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
