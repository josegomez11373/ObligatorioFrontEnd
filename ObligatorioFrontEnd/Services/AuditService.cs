using System.Text.Json;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class AuditService
{
    private readonly IUserScope userScope;
    private readonly List<AuditEntry> entries = [];

    public AuditService(IUserScope userScope) => this.userScope = userScope;
    public event Action? StateChanged;

    public IReadOnlyList<AuditEntry> GetAll() => entries
        .Where(CanSeeEntry)
        .OrderByDescending(entry => entry.FechaUtc)
        .ToList();

    public IReadOnlyList<AuditEntry> Search(string? entidad = null, string? actor = null, DateTime? desde = null, DateTime? hasta = null, Guid? empresaId = null)
    {
        var query = GetAll().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(entidad))
        {
            query = query.Where(entry => entry.Entidad.Equals(entidad.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            query = query.Where(entry => entry.Actor.Contains(actor.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (empresaId.HasValue)
        {
            query = query.Where(entry => entry.EmpresaId == empresaId);
        }

        if (desde.HasValue)
        {
            query = query.Where(entry => SigaTime.ToLocal(entry.FechaUtc).Date >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            query = query.Where(entry => SigaTime.ToLocal(entry.FechaUtc).Date <= hasta.Value.Date);
        }

        return query.OrderByDescending(entry => entry.FechaUtc).ToList();
    }

    public IReadOnlyList<string> GetEntidades() => GetAll()
        .Select(entry => entry.Entidad)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name)
        .ToList();

    public IReadOnlyList<string> GetActores() => GetAll()
        .Select(entry => entry.Actor)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name)
        .ToList();

    public void Record(string entity, Guid entityId, string action, Guid? empresaId, object? before, object? after)
    {
        var current = userScope.CurrentUser;
        if (empresaId is Guid companyId && !userScope.CanAccessEmpresa(companyId)) return;

        // Las acciones de un administrador de empresa siempre quedan asociadas a su empresa.
        if (empresaId is null && current is { Role: not UserRole.AdministradorGeneral } && current.EmpresaId is Guid ownCompany)
        {
            empresaId = ownCompany;
        }

        entries.Add(new AuditEntry
        {
            UsuarioId = current?.Id,
            Actor = current?.Email ?? "sistema",
            EmpresaId = empresaId,
            Entidad = entity,
            EntidadId = entityId,
            Accion = action,
            Antes = before is null ? null : JsonSerializer.Serialize(before),
            Despues = after is null ? null : JsonSerializer.Serialize(after)
        });

        StateChanged?.Invoke();
    }

    private bool CanSeeEntry(AuditEntry entry)
    {
        var current = userScope.CurrentUser;
        if (current is null) return false;
        if (current.Role == UserRole.AdministradorGeneral) return true;
        return entry.EmpresaId == current.EmpresaId;
    }
}
