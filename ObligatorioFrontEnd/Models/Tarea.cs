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
    public string Descripcion { get; init; } = string.Empty;
    public DateTime FechaPlanificada { get; init; }
    public DateTime? FechaRealizacion { get; init; }
    public string? Resultado { get; init; }
    public DateTime? FechaComprobacion { get; init; }
    public bool? SolucionoProblema { get; init; }
    public Guid? EjecutorId { get; init; }
    public TareaEjecutorTipo? EjecutorTipo { get; init; }
    public string? AsignadaPor { get; init; }
    public string? VerificadaPor { get; init; }
    public TareaEstado Estado { get; init; } = TareaEstado.Planificada;
    public string? MotivoAnulacion { get; init; }
    public decimal? CostoContratista { get; init; }
    public decimal? Costo { get; init; }
    public IReadOnlyList<TareaRecurso> Recursos { get; init; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public long Version { get; init; } = 1;
}
