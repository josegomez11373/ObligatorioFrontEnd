namespace ObligatorioFrontEnd.Models;

public sealed class ReportFilter
{
    public Guid? EmpresaId { get; set; }
    public Guid? EstablecimientoId { get; set; }
    public Guid? PotreroId { get; set; }
    public Guid? CampanaId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public CampanaEstado? CampanaEstado { get; set; }
    public EventoEstado? EventoEstado { get; set; }
    public TareaEstado? TareaEstado { get; set; }
}

public sealed record ReportDashboard
{
    public int Campanas { get; init; }
    public int CampanasActivas { get; init; }
    public int CampanasCerradas { get; init; }
    public int Eventos { get; init; }
    public int EventosRegistrados { get; init; }
    public int EventosAnulados { get; init; }
    public int Tareas { get; init; }
    public int TareasPendientes { get; init; }
    public int TareasRealizadas { get; init; }
    public int TareasVerificadas { get; init; }
    public decimal CostoTareas { get; init; }
    public int RecursosUtilizados { get; init; }
    public IReadOnlyList<ReportGroupRow> EventosPorTipo { get; init; } = [];
    public IReadOnlyList<ReportGroupRow> TareasPorTipo { get; init; } = [];
    public IReadOnlyList<ReportCampaignRow> CampanasDetalle { get; init; } = [];
    public IReadOnlyList<ReportActivityRow> ActividadReciente { get; init; } = [];
    public IReadOnlyList<ReportTimelineRow> LineaDeTiempo { get; init; } = [];
    public IReadOnlyList<ReportProblemRow> ProblemasDetectados { get; init; } = [];
    public IReadOnlyList<ReportEventAuthorRow> EventosConAutor { get; init; } = [];
    public IReadOnlyList<ReportTaskResponseRow> TareasDeRespuesta { get; init; } = [];
    public IReadOnlyList<ReportResourceRow> RecursosDetalle { get; init; } = [];
    public IReadOnlyList<ReportCampaignCostRow> CostosPorCampana { get; init; } = [];
    public IReadOnlyList<ReportTreatmentRow> Tratamientos { get; init; } = [];
    public IReadOnlyList<ReportZoneRow> ZonasProblematicas { get; init; } = [];
}

public sealed record ReportGroupRow(string Nombre, int Cantidad, decimal Importe = 0);

public sealed record ReportCampaignRow(
    Guid Id,
    string Nombre,
    string Empresa,
    CampanaEstado Estado,
    DateTime FechaInicio,
    int Potreros,
    int Eventos,
    int Tareas,
    decimal CostoTareas);

public sealed record ReportActivityRow(
    DateTime FechaUtc,
    string Actor,
    string Entidad,
    string Accion,
    Guid? EmpresaId);

public sealed record ReportTimelineRow(
    DateTime Fecha,
    string Tipo,
    string Potrero,
    string Descripcion,
    string Actor,
    string Origen,
    decimal? Costo);

public sealed record ReportProblemRow(
    string Tipo,
    string Potrero,
    int Cantidad,
    DateTime? UltimaDeteccion);

public sealed record ReportEventAuthorRow(
    DateTime Fecha,
    string Tipo,
    string Potrero,
    string Autor,
    EventoEstado Estado);

public sealed record ReportTaskResponseRow(
    DateTime Fecha,
    string Evento,
    string Tarea,
    string Potrero,
    TareaEstado Estado,
    string? Resultado,
    decimal Costo);

public sealed record ReportResourceRow(
    string Tipo,
    string Nombre,
    string Unidad,
    decimal Cantidad,
    decimal Costo);

public sealed record ReportCampaignCostRow(
    string Campana,
    string Empresa,
    int Tareas,
    int TareasAnuladas,
    decimal Insumos,
    decimal Maquinaria,
    decimal Total);

public sealed record ReportTreatmentRow(
    string Tratamiento,
    int Total,
    int Resueltas,
    double Efectividad,
    decimal CostoPromedio);

public sealed record ReportZoneRow(
    string Establecimiento,
    string Potrero,
    int Eventos,
    string TiposFrecuentes);
