using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Establecimiento
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public double? SuperficieHa { get; init; }
    public string? Ubicacion { get; init; }
    public string? Responsables { get; init; }
    public bool Activo { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public long Version { get; init; } = 1;
}
