using System;

namespace ObligatorioFrontEnd.Models;

public enum TareaEstado
{
    Planificada,
    Asignada,
    Realizada,
    Verificada,
    Anulada
}

public enum TareaEjecutorTipo
{
    Usuario,
    Contratista
}

public sealed record Tarea
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public Guid CampanaPotreroId { get; init; }
    public Guid? EventoId { get; init; }
    public Guid TipoTareaId { get; init; }
    public Guid? EjecutorId { get; init; }
    public TareaEjecutorTipo? EjecutorTipo { get; init; }
    public TareaEstado Estado { get; init; } = TareaEstado.Planificada;
    public decimal? Costo { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public long Version { get; init; } = 1;
}
