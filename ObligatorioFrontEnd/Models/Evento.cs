using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Evento
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public Guid CampanaPotreroId { get; init; }
    public Guid TipoEventoId { get; init; }
    public DateTime Fecha { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string? Ubicacion { get; init; }
    public double? Latitud { get; init; }
    public double? Longitud { get; init; }
    public string? RegistradoPor { get; init; }
    public EventoEstado Estado { get; init; } = EventoEstado.Registrado;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public long Version { get; init; } = 1;
}
