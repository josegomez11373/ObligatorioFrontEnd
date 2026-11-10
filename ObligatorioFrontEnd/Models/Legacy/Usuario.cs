using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Usuario
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Email { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public UserRole Rol { get; init; }
    public Guid? EmpresaId { get; init; }
    public IReadOnlyList<Guid> EstablecimientoIds { get; init; } = [];
    public bool Activo { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public long Version { get; init; } = 1;
}
