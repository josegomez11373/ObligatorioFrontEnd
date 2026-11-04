using System;

namespace ObligatorioFrontEnd.Models;

public sealed record AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime FechaUtc { get; init; } = DateTime.UtcNow;
    public Guid? UsuarioId { get; init; }
    public string Actor { get; init; } = string.Empty;
    public Guid? EmpresaId { get; init; }
    public string Entidad { get; init; } = string.Empty;
    public Guid EntidadId { get; init; }
    public string Accion { get; init; } = string.Empty;
    public string? Antes { get; init; }
    public string? Despues { get; init; }
}
