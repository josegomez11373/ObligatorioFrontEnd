using System;

namespace ObligatorioFrontEnd.Models;

public sealed record CampanaPotrero
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CampanaId { get; init; }
    public Guid PotreroId { get; init; }
    public DateTime FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public CampanaEstado Estado { get; init; } = CampanaEstado.Planificada;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public long Version { get; init; } = 1;
}
