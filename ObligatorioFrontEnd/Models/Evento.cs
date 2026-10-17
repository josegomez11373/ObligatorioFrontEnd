using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Evento
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CampanaPotreroId { get; init; }
    public Guid TipoEventoId { get; init; }
    public DateTime Fecha { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string? Ubicacion { get; init; }
}
