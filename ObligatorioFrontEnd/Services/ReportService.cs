using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class ReportService
{
    private readonly CampanaService campanaService;
    private readonly EventoService eventoService;
    private readonly TipoEventoService tipoEventoService;
    private readonly TipoTareaService tipoTareaService;
    private readonly TareaService tareaService;
    private readonly AuditService auditService;
    private readonly EmpresaService empresaService;
    private readonly EstablecimientoService establecimientoService;
    private readonly PotreroService potreroService;
    private readonly InsumoService insumoService;
    private readonly MaquinariaService maquinariaService;

    public ReportService(
        CampanaService campanaService,
        EventoService eventoService,
        TipoEventoService tipoEventoService,
        TipoTareaService tipoTareaService,
        TareaService tareaService,
        AuditService auditService,
        EmpresaService empresaService,
        EstablecimientoService establecimientoService,
        PotreroService potreroService,
        InsumoService insumoService,
        MaquinariaService maquinariaService)
    {
        this.campanaService = campanaService;
        this.eventoService = eventoService;
        this.tipoEventoService = tipoEventoService;
        this.tipoTareaService = tipoTareaService;
        this.tareaService = tareaService;
        this.auditService = auditService;
        this.empresaService = empresaService;
        this.establecimientoService = establecimientoService;
        this.potreroService = potreroService;
        this.insumoService = insumoService;
        this.maquinariaService = maquinariaService;
    }

    public ReportDashboard Build(ReportFilter filter)
    {
        var campanas = campanaService.GetAll()
            .Where(c => (!filter.EmpresaId.HasValue || c.EmpresaId == filter.EmpresaId) &&
                        (!filter.CampanaId.HasValue || c.Id == filter.CampanaId) &&
                        (!filter.CampanaEstado.HasValue || c.Estado == filter.CampanaEstado) &&
                        IsWithin(c.FechaInicio, filter.Desde, filter.Hasta))
            .ToList();

        var campanaIds = campanas.Select(c => c.Id).ToHashSet();
        var assignments = campanaService.GetCampanaPotreros()
            .Where(item => campanaIds.Contains(item.Campana.Id))
            .Where(item =>
            {
                var potrero = potreroService.GetById(item.Assignment.PotreroId);
                return potrero is not null &&
                       (!filter.EstablecimientoId.HasValue || potrero.EstablecimientoId == filter.EstablecimientoId) &&
                       (!filter.PotreroId.HasValue || item.Assignment.PotreroId == filter.PotreroId);
            })
            .ToList();

        var assignmentIds = assignments.Select(item => item.Assignment.Id).ToHashSet();
        var assignmentLookup = assignments.ToDictionary(item => item.Assignment.Id);

        var eventos = eventoService.GetAll()
            .Where(e => assignmentIds.Contains(e.CampanaPotreroId) &&
                        (!filter.EventoEstado.HasValue || e.Estado == filter.EventoEstado) &&
                        IsWithin(e.Fecha, filter.Desde, filter.Hasta))
            .ToList();

        var tareas = tareaService.GetAll()
            .Where(t => assignmentIds.Contains(t.CampanaPotreroId) &&
                        (!filter.TareaEstado.HasValue || t.Estado == filter.TareaEstado) &&
                        IsWithin(t.FechaPlanificada, filter.Desde, filter.Hasta))
            .ToList();

        var tareasVigentes = tareas.Where(t => t.Estado != TareaEstado.Anulada).ToList();
        var eventosVigentes = eventos.Where(e => e.Estado != EventoEstado.Anulado).ToList();

        var campaignRows = campanas
            .Select(campaign =>
            {
                var campaignAssignments = assignments.Where(item => item.Campana.Id == campaign.Id).ToList();
                if (campaignAssignments.Count == 0 && (filter.EstablecimientoId.HasValue || filter.PotreroId.HasValue))
                {
                    return null;
                }

                var campaignAssignmentIds = campaignAssignments.Select(item => item.Assignment.Id).ToHashSet();
                var campaignTasks = tareas.Where(task => campaignAssignmentIds.Contains(task.CampanaPotreroId) && task.Estado != TareaEstado.Anulada).ToList();
                return new ReportCampaignRow(
                    campaign.Id,
                    campaign.Nombre,
                    empresaService.GetById(campaign.EmpresaId)?.Nombre ?? "—",
                    campaign.Estado,
                    campaign.FechaInicio,
                    campaignAssignments.Count,
                    eventos.Count(item => campaignAssignmentIds.Contains(item.CampanaPotreroId)),
                    campaignTasks.Count,
                    campaignTasks.Sum(task => task.Costo ?? 0));
            })
            .Where(row => row is not null)
            .Select(row => row!)
            .OrderByDescending(row => row.FechaInicio)
            .ToList();

        var costRows = campanas
            .Select(campaign =>
            {
                var campaignAssignmentIds = assignments
                    .Where(item => item.Campana.Id == campaign.Id)
                    .Select(item => item.Assignment.Id)
                    .ToHashSet();
                var campaignTasks = tareas.Where(task => campaignAssignmentIds.Contains(task.CampanaPotreroId)).ToList();
                var vigentes = campaignTasks.Where(task => task.Estado != TareaEstado.Anulada).ToList();
                var insumos = vigentes.SelectMany(task => task.Recursos)
                    .Where(resource => resource.Tipo == TareaRecursoTipo.Insumo)
                    .Sum(resource => resource.Costo ?? 0);
                var maquinaria = vigentes.SelectMany(task => task.Recursos)
                    .Where(resource => resource.Tipo == TareaRecursoTipo.Maquinaria)
                    .Sum(resource => resource.Costo ?? 0);
                var contratistas = vigentes.Sum(task => task.CostoContratista ?? 0);
                return new ReportCampaignCostRow(
                    campaign.Nombre,
                    empresaService.GetById(campaign.EmpresaId)?.Nombre ?? "—",
                    vigentes.Count,
                    campaignTasks.Count - vigentes.Count,
                    insumos,
                    maquinaria,
                    insumos + maquinaria + contratistas);
            })
            .OrderByDescending(row => row.Total)
            .ToList();

        var timeline = eventos
            .Select(evento =>
            {
                var context = assignmentLookup[evento.CampanaPotreroId];
                return new ReportTimelineRow(
                    evento.Fecha,
                    "Evento",
                    PotreroName(context.Assignment.PotreroId),
                    evento.Descripcion,
                    evento.RegistradoPor ?? evento.CreatedBy ?? "—",
                    tipoEventoService.GetById(evento.TipoEventoId)?.Nombre ?? "Evento",
                    null);
            })
            .Concat(tareas.Select(tarea =>
            {
                var context = assignmentLookup[tarea.CampanaPotreroId];
                return new ReportTimelineRow(
                    tarea.FechaPlanificada,
                    "Tarea",
                    PotreroName(context.Assignment.PotreroId),
                    tarea.Descripcion,
                    tarea.AsignadaPor ?? tarea.CreatedBy ?? "—",
                    tipoTareaService.GetById(tarea.TipoTareaId)?.Nombre ?? "Tarea",
                    tarea.Costo);
            }))
            .OrderByDescending(row => row.Fecha)
            .ToList();

        var problemEvents = eventosVigentes
            .Where(evento => IsProblemType(tipoEventoService.GetById(evento.TipoEventoId)?.Nombre))
            .ToList();

        var problems = problemEvents
            .GroupBy(evento => new
            {
                Tipo = tipoEventoService.GetById(evento.TipoEventoId)?.Nombre ?? "Sin tipo",
                Potrero = PotreroName(assignmentLookup[evento.CampanaPotreroId].Assignment.PotreroId)
            })
            .Select(group => new ReportProblemRow(
                group.Key.Tipo,
                group.Key.Potrero,
                group.Count(),
                group.Max(evento => (DateTime?)evento.Fecha)))
            .OrderByDescending(row => row.Cantidad)
            .ToList();

        var eventAuthors = eventos
            .Select(evento => new ReportEventAuthorRow(
                evento.Fecha,
                tipoEventoService.GetById(evento.TipoEventoId)?.Nombre ?? "Sin tipo",
                PotreroName(assignmentLookup[evento.CampanaPotreroId].Assignment.PotreroId),
                evento.RegistradoPor ?? evento.CreatedBy ?? "—",
                evento.Estado))
            .OrderByDescending(row => row.Fecha)
            .ToList();

        var taskResponses = tareas
            .Where(tarea => tarea.EventoId.HasValue)
            .Select(tarea =>
            {
                var evento = eventoService.GetById(tarea.EventoId!.Value);
                var context = assignmentLookup[tarea.CampanaPotreroId];
                return new ReportTaskResponseRow(
                    tarea.FechaPlanificada,
                    evento is null ? "Evento eliminado" : $"{tipoEventoService.GetById(evento.TipoEventoId)?.Nombre ?? "Evento"}: {evento.Descripcion}",
                    tipoTareaService.GetById(tarea.TipoTareaId)?.Nombre ?? "Tarea",
                    PotreroName(context.Assignment.PotreroId),
                    tarea.Estado,
                    tarea.Resultado,
                    tarea.Costo ?? 0);
            })
            .OrderByDescending(row => row.Fecha)
            .ToList();

        var resources = tareasVigentes
            .SelectMany(tarea => tarea.Recursos)
            .Select(resource => new
            {
                Resource = resource,
                Name = resource.Tipo == TareaRecursoTipo.Insumo
                    ? insumoService.GetById(resource.RecursoId)?.Nombre ?? "Insumo eliminado"
                    : maquinariaService.GetById(resource.RecursoId)?.Nombre ?? "Maquinaria eliminada",
                Unit = resource.Tipo == TareaRecursoTipo.Insumo
                    ? insumoService.GetById(resource.RecursoId)?.Unidad ?? "—"
                    : "hora"
            })
            .GroupBy(item => new { item.Resource.Tipo, item.Name, item.Unit })
            .Select(group => new ReportResourceRow(
                group.Key.Tipo == TareaRecursoTipo.Insumo ? "Insumo" : "Maquinaria",
                group.Key.Name,
                group.Key.Unit,
                group.Sum(item => item.Resource.Cantidad),
                group.Sum(item => item.Resource.Costo ?? 0)))
            .OrderByDescending(row => row.Costo)
            .ToList();

        var treatments = tareasVigentes
            .GroupBy(tarea => tipoTareaService.GetById(tarea.TipoTareaId)?.Nombre ?? "Sin tipo")
            .Select(group =>
            {
                var total = group.Count();
                var resueltas = group.Count(tarea => tarea.SolucionoProblema == true);
                var costoPromedio = total == 0 ? 0 : group.Sum(tarea => tarea.Costo ?? 0) / total;
                return new ReportTreatmentRow(
                    group.Key,
                    total,
                    resueltas,
                    total == 0 ? 0 : resueltas * 100d / total,
                    costoPromedio);
            })
            .OrderByDescending(row => row.Efectividad)
            .ThenByDescending(row => row.Total)
            .ToList();

        var zones = problemEvents
            .Select(evento =>
            {
                var context = assignmentLookup[evento.CampanaPotreroId];
                var potrero = potreroService.GetById(context.Assignment.PotreroId);
                return new
                {
                    Establecimiento = potrero is null ? "—" : establecimientoService.GetById(potrero.EstablecimientoId)?.Nombre ?? "—",
                    Potrero = potrero?.Nombre ?? "—",
                    Tipo = tipoEventoService.GetById(evento.TipoEventoId)?.Nombre ?? "Sin tipo"
                };
            })
            .GroupBy(item => new { item.Establecimiento, item.Potrero })
            .Select(group => new ReportZoneRow(
                group.Key.Establecimiento,
                group.Key.Potrero,
                group.Count(),
                string.Join(", ", group.GroupBy(item => item.Tipo).OrderByDescending(item => item.Count()).Select(item => $"{item.Key} ({item.Count()})").Take(3))))
            .OrderByDescending(row => row.Eventos)
            .ToList();

        return new ReportDashboard
        {
            Campanas = campanas.Count,
            CampanasActivas = campanas.Count(c => c.Estado == CampanaEstado.Activa),
            CampanasCerradas = campanas.Count(c => c.Estado == CampanaEstado.Cerrada),
            Eventos = eventos.Count,
            EventosRegistrados = eventos.Count(e => e.Estado == EventoEstado.Registrado),
            EventosAnulados = eventos.Count(e => e.Estado == EventoEstado.Anulado),
            Tareas = tareas.Count,
            TareasPendientes = tareas.Count(t => t.Estado is TareaEstado.Planificada or TareaEstado.Asignada),
            TareasRealizadas = tareas.Count(t => t.Estado == TareaEstado.Realizada),
            TareasVerificadas = tareas.Count(t => t.Estado == TareaEstado.Verificada),
            CostoTareas = tareasVigentes.Sum(t => t.Costo ?? 0),
            RecursosUtilizados = tareasVigentes.Sum(t => t.Recursos.Count),
            EventosPorTipo = eventos
                .GroupBy(e => tipoEventoService.GetById(e.TipoEventoId)?.Nombre ?? "Sin tipo")
                .Select(group => new ReportGroupRow(group.Key, group.Count()))
                .OrderByDescending(row => row.Cantidad)
                .ToList(),
            TareasPorTipo = tareas
                .GroupBy(t => tipoTareaService.GetById(t.TipoTareaId)?.Nombre ?? t.Estado.ToString())
                .Select(group => new ReportGroupRow(group.Key, group.Count(), group.Where(t => t.Estado != TareaEstado.Anulada).Sum(t => t.Costo ?? 0)))
                .OrderByDescending(row => row.Cantidad)
                .ToList(),
            CampanasDetalle = campaignRows,
            ActividadReciente = auditService.GetAll()
                .Where(entry => (!filter.EmpresaId.HasValue || entry.EmpresaId == filter.EmpresaId) &&
                                IsWithin(SigaTime.ToLocal(entry.FechaUtc), filter.Desde, filter.Hasta))
                .Take(20)
                .Select(entry => new ReportActivityRow(entry.FechaUtc, entry.Actor, entry.Entidad, entry.Accion, entry.EmpresaId))
                .ToList(),
            LineaDeTiempo = timeline,
            ProblemasDetectados = problems,
            EventosConAutor = eventAuthors,
            TareasDeRespuesta = taskResponses,
            RecursosDetalle = resources,
            CostosPorCampana = costRows,
            Tratamientos = treatments,
            ZonasProblematicas = zones
        };
    }

    private string PotreroName(Guid potreroId)
    {
        if (potreroId == Guid.Empty) return "—";
        return potreroService.GetById(potreroId)?.Nombre ?? "—";
    }

    private static bool IsProblemType(string? typeName) =>
        typeName is not null &&
        (typeName.Contains("plaga", StringComparison.OrdinalIgnoreCase) ||
         typeName.Contains("maleza", StringComparison.OrdinalIgnoreCase) ||
         typeName.Contains("enfermedad", StringComparison.OrdinalIgnoreCase));

    private static bool IsWithin(DateTime value, DateTime? from, DateTime? to)
    {
        var date = value.Date;
        return (!from.HasValue || date >= from.Value.Date) && (!to.HasValue || date <= to.Value.Date);
    }
}
