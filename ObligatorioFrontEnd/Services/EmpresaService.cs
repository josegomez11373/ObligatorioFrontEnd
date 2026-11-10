using System;
using System.Collections.Generic;
using System.Linq;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class EmpresaService : IServicioEmpresas
{
    private readonly IUserScope userScope;
    private readonly AuditService audit;
    private readonly NotificadorDatos notificador;
    private readonly List<Empresa> empresas = new()
    {
        new Empresa { Id = SeedIds.AgroDelSur, Nombre = "Agro del Sur", CreatedBy = "seed" },
        new Empresa { Id = SeedIds.CamposVerdes, Nombre = "Campos Verdes", CreatedBy = "seed" },
    };

    public EmpresaService(IUserScope userScope, AuditService audit, NotificadorDatos notificador)
    {
        this.userScope = userScope;
        this.audit = audit;
        this.notificador = notificador;
    }

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
        ValidateNombre(nombre, Guid.Empty);

        var emp = new Empresa
        {
            Nombre = nombre.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userScope.CurrentUser?.Email
        };
        empresas.Add(emp);
        audit.Record(nameof(Empresa), emp.Id, "Crear", emp.Id, null, emp);
        Notify();
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
        ValidateNombre(nombre, existing.Id);

        var replacement = existing with
        {
            Nombre = nombre.Trim(),
            Activo = activo,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        empresas[idx] = replacement;
        audit.Record(nameof(Empresa), existing.Id, activo ? "Actualizar" : "Desactivar", existing.Id, existing, replacement);
        Notify();
        return true;
    }

    public bool Deactivate(Guid id)
    {
        EnsureCanManage();
        var idx = empresas.FindIndex(e => e.Id == id);
        if (idx < 0) return false;

        var existing = empresas[idx];
        EnsureEmpresaAccess(existing.Id);
        if (!existing.Activo) return true;

        var replacement = existing with
        {
            Activo = false,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userScope.CurrentUser?.Email,
            Version = existing.Version + 1
        };
        empresas[idx] = replacement;
        audit.Record(nameof(Empresa), existing.Id, "Desactivar", existing.Id, existing, replacement);
        Notify();
        return true;
    }

    public Task<IReadOnlyList<EmpresaDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EmpresaDto> result = GetAll()
            .Where(empresa => includeInactive || empresa.Activo)
            .Select(empresa => empresa.AContrato())
            .ToList();

        return Task.FromResult(result);
    }

    public Task<EmpresaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GetById(id)?.AContrato());

    public Task<EmpresaDto> CreateAsync(
        string nombre,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Create(nombre).AContrato());

    public Task<bool> UpdateAsync(
        Guid id,
        string nombre,
        bool activo,
        long expectedVersion = 0,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Update(id, nombre, activo, expectedVersion));

    public Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Deactivate(id));

    private void Notify()
    {
        StateChanged?.Invoke();
        notificador.Notificar(ModulosDatos.Empresas);
    }

    private void ValidateNombre(string nombre, Guid ignoredId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre de la empresa es obligatorio.");
        }

        var trimmed = nombre.Trim();
        if (empresas.Any(e => e.Id != ignoredId && e.Nombre.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ya existe una empresa con ese nombre.");
        }
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
