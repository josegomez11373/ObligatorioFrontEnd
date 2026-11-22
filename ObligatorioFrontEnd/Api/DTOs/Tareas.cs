namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Recurso consumido por una tarea, tal como lo devuelve la API.</summary>
public sealed record TareaRecursoDto(
    TipoRecursoTarea Tipo,
    Guid RecursoId,
    string? Nombre,
    decimal Cantidad,
    decimal? CostoUnitario,
    decimal? CostoTotal);

/// <summary>
/// Tarea tal como la devuelve la API. La campaña se deduce cruzando
/// <see cref="CampanaPotreroId"/> con <see cref="CampanaDto.Potreros"/>.
/// </summary>
public sealed record TareaDto(
    Guid Id,
    Guid EmpresaId,
    Guid CampanaPotreroId,
    string? PotreroNombre,
    Guid? EventoId,
    Guid TipoTareaId,
    string? TipoTareaNombre,
    string? Descripcion,
    DateTime? FechaPlanificada,
    DateTime? FechaRealizacion,
    string? Resultado,
    DateTime? FechaComprobacion,
    bool? SolucionoProblema,
    Guid? EjecutorId,
    string? EjecutorNombre,
    string? EjecutorTipo,
    string? AsignadaPor,
    string? VerificadaPor,
    EstadoTarea Estado,
    string? MotivoAnulacion,
    decimal? Costo,
    DateTime FechaCreacion,
    long Version)
{
    private readonly IReadOnlyList<TareaRecursoDto>? recursos;

    /// <summary>Se normaliza a vacía si la API omite o envía <c>null</c>.</summary>
    public IReadOnlyList<TareaRecursoDto> Recursos
    {
        get => recursos ?? [];
        init => recursos = value;
    }
}

public sealed class CrearTareaDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public Guid CampanaPotreroId { get; set; }
    public Guid? EventoId { get; set; }
    public Guid TipoTareaId { get; set; }
    public string? Descripcion { get; set; } = string.Empty;
    public DateTime? FechaPlanificada { get; set; } = DateTime.UtcNow.Date;
}

public sealed class ActualizarTareaDto
{
    public Guid Id { get; set; }
    public Guid TipoTareaId { get; set; }
    public string? Descripcion { get; set; } = string.Empty;
    public DateTime? FechaPlanificada { get; set; }
    public long Version { get; set; }
}

public sealed class AsignarTareaDto
{
    public Guid? AsignadaAUsuarioId { get; set; }
    public Guid? AsignadaAContratistaId { get; set; }
    public DateTime? FechaPlanificada { get; set; }
    public long Version { get; set; }
}

/// <summary>Recurso que se registra al marcar la tarea como realizada.</summary>
public sealed class RecursoUtilizadoDto
{
    public TipoRecursoTarea Tipo { get; set; }
    public Guid RecursoId { get; set; }
    public decimal Cantidad { get; set; }
}

public sealed class RealizarTareaDto
{
    public string Resultado { get; set; } = string.Empty;
    public DateTime? FechaRealizacion { get; set; }
    public bool? SolucionoProblema { get; set; }
    public List<RecursoUtilizadoDto> Recursos { get; set; } = [];
    public long Version { get; set; }
}

public sealed class VerificarTareaDto
{
    public bool SolucionoProblema { get; set; }
    public DateTime? FechaComprobacion { get; set; }
    public long Version { get; set; }
}

public sealed class AnularTareaDto
{
    public string MotivoAnulacion { get; set; } = string.Empty;
    public long Version { get; set; }
}
