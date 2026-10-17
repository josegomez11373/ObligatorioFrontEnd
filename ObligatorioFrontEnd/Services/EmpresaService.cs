using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class EmpresaService
{
    private readonly IUserScope userScope;
    private readonly List<Empresa> empresas = new()
    {
        new Empresa { Id = SeedIds.AgroDelSur, Nombre = "Agro del Sur", CreatedBy = "seed" },
        new Empresa { Id = SeedIds.CamposVerdes, Nombre = "Campos Verdes", CreatedBy = "seed" },
    };

    public EmpresaService(IUserScope userScope) => this.userScope = userScope;

    public event Action? StateChanged;

    public IReadOnlyList<Empresa> GetAll() => empresas
        .Where(e => userScope.CanAccessEmpresa(e.Id))
        .OrderBy(e => e.Nombre)
        .ToList();

    public Empresa? GetById(Guid id) => userScope.CanAccessEmpresa(id)
        ? empresas.SingleOrDefault(e => e.Id == id)
        : null;

    public Empresa Create(string nombre)
    {
        EnsureCanManage();
        var emp = new Empresa { Nombre = nombre, CreatedBy = userScope.CurrentUser?.Email };
        empresas.Add(emp);
        StateChanged?.Invoke();
        return emp;
    }

    public bool Update(Guid id, string nombre, bool activo, long expectedVersion = 0)
    {
        EnsureCanManage();
        var idx = empresas.FindIndex(e => e.Id == id);
        if (idx < 0) return false;
        var existing = empresas[idx];
        EnsureEmpresaAccess(existing.Id);
        EnsureVersion(existing.Version, expectedVersion);
        empresas[idx] = existing with
        {
            Nombre = nombre,
            Activo = activo,
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
        var idx = empresas.FindIndex(e => e.Id == id);
        if (idx < 0) return false;
        var existing = empresas[idx];
        EnsureEmpresaAccess(existing.Id);
        empresas[idx] = existing with { Activo = false, UpdatedAt = DateTime.UtcNow, UpdatedBy = userScope.CurrentUser?.Email, Version = existing.Version + 1 };
        StateChanged?.Invoke();
        return true;
    }

    private void EnsureCanManage()
    {
        if (userScope.CurrentUser is not { Role: var role } || !Permissions.CanManage(role, SigaModule.Empresas))
        {
            throw new InvalidOperationException("No tiene permisos para gestionar empresas.");
        }
    }

    private void EnsureEmpresaAccess(Guid empresaId)
    {
        if (!userScope.CanAccessEmpresa(empresaId))
        {
            throw new InvalidOperationException("No tiene acceso a la empresa seleccionada.");
        }
    }

    private static void EnsureVersion(long currentVersion, long expectedVersion)
    {
        if (expectedVersion != 0 && currentVersion != expectedVersion)
        {
            throw new InvalidOperationException("El registro fue modificado por otro usuario. Recargue la información e intente nuevamente.");
    }
    }
}
