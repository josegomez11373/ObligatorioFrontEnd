using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Campana
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PotreroId { get; init; }
    public Guid CultivoId { get; init; }
    public DateTime FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public bool Activa { get; init; } = true;
}
