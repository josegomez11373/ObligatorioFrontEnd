using System;

namespace ObligatorioFrontEnd.Models;

public enum TareaEstado
{
    Pendiente,
    EnEjecucion,
    Verificada,
    Cancelada
}

public sealed record Tarea
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? EventoId { get; init; }
    public Guid TipoTareaId { get; init; }
    public Guid? ResponsableId { get; init; }
    public TareaEstado Estado { get; init; } = TareaEstado.Pendiente;
    public decimal? Costo { get; init; }
}
