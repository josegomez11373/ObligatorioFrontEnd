using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Agregaciones puras del tablero de reportes. No conoce HTTP: recibe los
/// listados ya descargados y devuelve las filas que consumen las pantallas.
/// </summary>
public static class CalculoReportes
{
    public static ReportDashboard Build(
        ReportFilter filtro,
        IReadOnlyList<CampanaDto> campanas,
        IReadOnlyList<PotreroDto> potreros,
        IReadOnlyList<EmpresaDto> empresas,
        IReadOnlyList<EstablecimientoDto> establecimientos,
        IReadOnlyList<EventoDto> eventos,
        IReadOnlyList<TareaDto> tareas,
        IReadOnlyList<AuditoriaDto> auditoria)
    {
        var empresasPorId = empresas.ToDictionary(empresa => empresa.Id);
        var potrerosPorId = potreros.ToDictionary(potrero => potrero.Id);
        var establecimientosPorId = establecimientos.ToDictionary(establecimiento => establecimiento.Id);

        var campanasFiltradas = campanas
            .Where(campana =>
                (!filtro.EmpresaId.HasValue || campana.EmpresaId == filtro.EmpresaId) &&
                (!filtro.CampanaId.HasValue || campana.Id == filtro.CampanaId) &&
                (!filtro.CampanaEstado.HasValue || campana.Estado == filtro.CampanaEstado) &&
                IsWithin(campana.FechaInicio, filtro.Desde, filtro.Hasta))
            .ToList();

        var campanaIds = campanasFiltradas.Select(campana => campana.Id).ToHashSet();

        var asignaciones = campanasFiltradas
            .SelectMany(campana => campana.Potreros.Select(potrero => new
            {
                CampanaId = campana.Id,
                Asignacion = potrero
            }))
            .Where(item =>
            {
                if (!potrerosPorId.TryGetValue(item.Asignacion.PotreroId, out var potrero))
                {
                    return !filtro.EstablecimientoId.HasValue && !filtro.PotreroId.HasValue;
                }

                return (!filtro.EstablecimientoId.HasValue ||
                        potrero.EstablecimientoId == filtro.EstablecimientoId) &&
                       (!filtro.PotreroId.HasValue || item.Asignacion.PotreroId == filtro.PotreroId);
            })
            .ToList();

        var asignacionesPorId = asignaciones
            .GroupBy(item => item.Asignacion.Id)
            .ToDictionary(group => group.Key, group => group.First());

        var asignacionIds = asignacionesPorId.Keys.ToHashSet();

        var eventosTodos = eventos.ToList();
        var eventosFiltrados = eventosTodos
            .Where(evento =>
                asignacionIds.Contains(evento.CampanaPotreroId) &&
                (!filtro.EventoEstado.HasValue || evento.Estado == filtro.EventoEstado) &&
                IsWithin(evento.Fecha, filtro.Desde, filtro.Hasta))
            .ToList();

        var tareasFiltradas = tareas
            .Where(tarea =>
                asignacionIds.Contains(tarea.CampanaPotreroId) &&
                (!filtro.TareaEstado.HasValue || tarea.Estado == filtro.TareaEstado) &&
                tarea.FechaPlanificada is DateTime fecha &&
                IsWithin(fecha, filtro.Desde, filtro.Hasta))
            .ToList();

        var tareasVigentes = tareasFiltradas
            .Where(tarea => tarea.Estado != EstadoTarea.Anulada)
            .ToList();
        var eventosVigentes = eventosFiltrados
            .Where(evento => evento.Estado != EstadoEvento.Anulado)
            .ToList();

        var eventosPorId = eventosTodos
            .GroupBy(evento => evento.Id)
            .ToDictionary(group => group.Key, group => group.First());

        string PotreroName(Guid potreroId)
        {
            if (potreroId == Guid.Empty)
            {
                return "—";
            }

            return potrerosPorId.TryGetValue(potreroId, out var potrero)
                ? potrero.Nombre
                : "—";
        }

        string EstablecimientoName(Guid potreroId)
        {
            if (!potrerosPorId.TryGetValue(potreroId, out var potrero))
            {
                return "—";
            }

            return establecimientosPorId.TryGetValue(potrero.EstablecimientoId, out var establecimiento)
                ? establecimiento.Nombre
                : potrero.EstablecimientoNombre ?? "—";
        }

        string EmpresaName(Guid empresaId) =>
            empresasPorId.TryGetValue(empresaId, out var empresa) ? empresa.Nombre : "—";

        var campaignRows = campanasFiltradas
            .Select(campana =>
            {
                var campanaAsignaciones = asignaciones
                    .Where(item => item.CampanaId == campana.Id)
                    .ToList();

                if (campanaAsignaciones.Count == 0 &&
                    (filtro.EstablecimientoId.HasValue || filtro.PotreroId.HasValue))
                {
                    return null;
                }

                var campanaAsignacionIds = campanaAsignaciones
                    .Select(item => item.Asignacion.Id)
                    .ToHashSet();
                var campanaTareas = tareasFiltradas
                    .Where(tarea => campanaAsignacionIds.Contains(tarea.CampanaPotreroId) &&
                                    tarea.Estado != EstadoTarea.Anulada)
                    .ToList();

                return new ReportCampaignRow(
                    campana.Id,
                    campana.Nombre,
                    EmpresaName(campana.EmpresaId),
                    campana.Estado,
                    campana.FechaInicio,
                    campanaAsignaciones.Count,
                    eventosFiltrados.Count(item => campanaAsignacionIds.Contains(item.CampanaPotreroId)),
                    campanaTareas.Count,
                    campanaTareas.Sum(tarea => tarea.Costo ?? 0));
            })
            .Where(row => row is not null)
            .Select(row => row!)
            .OrderByDescending(row => row.FechaInicio)
            .ToList();

        var costRows = campanasFiltradas
            .Select(campana =>
            {
                var campanaAsignacionIds = asignaciones
                    .Where(item => item.CampanaId == campana.Id)
                    .Select(item => item.Asignacion.Id)
                    .ToHashSet();
                var campanaTareas = tareasFiltradas
                    .Where(tarea => campanaAsignacionIds.Contains(tarea.CampanaPotreroId))
                    .ToList();
                var vigentes = campanaTareas
                    .Where(tarea => tarea.Estado != EstadoTarea.Anulada)
                    .ToList();
                var insumos = vigentes
                    .SelectMany(tarea => tarea.Recursos)
                    .Where(recurso => recurso.Tipo == TipoRecursoTarea.Insumo)
                    .Sum(recurso => recurso.CostoTotal ?? 0);
                var maquinaria = vigentes
                    .SelectMany(tarea => tarea.Recursos)
                    .Where(recurso => recurso.Tipo == TipoRecursoTarea.Maquinaria)
                    .Sum(recurso => recurso.CostoTotal ?? 0);

                return new ReportCampaignCostRow(
                    campana.Nombre,
                    EmpresaName(campana.EmpresaId),
                    vigentes.Count,
                    campanaTareas.Count - vigentes.Count,
                    insumos,
                    maquinaria,
                    insumos + maquinaria);
            })
            .OrderByDescending(row => row.Total)
            .ToList();

        var timeline = eventosFiltrados
            .Where(evento => asignacionesPorId.ContainsKey(evento.CampanaPotreroId))
            .Select(evento =>
            {
                var asignacion = asignacionesPorId[evento.CampanaPotreroId];
                return new ReportTimelineRow(
                    evento.Fecha,
                    "Evento",
                    PotreroName(asignacion.Asignacion.PotreroId),
                    evento.Descripcion ?? "—",
                    evento.RegistradoPor ?? "—",
                    evento.TipoEventoNombre ?? "Evento",
                    null);
            })
            .Concat(tareasFiltradas
                .Where(tarea => asignacionesPorId.ContainsKey(tarea.CampanaPotreroId))
                .Select(tarea =>
                {
                    var asignacion = asignacionesPorId[tarea.CampanaPotreroId];
                    return new ReportTimelineRow(
                        tarea.FechaPlanificada ?? tarea.FechaCreacion,
                        "Tarea",
                        PotreroName(asignacion.Asignacion.PotreroId),
                        tarea.Descripcion ?? "—",
                        tarea.AsignadaPor ?? "—",
                        tarea.TipoTareaNombre ?? "Tarea",
                        tarea.Costo);
                }))
            .OrderByDescending(row => row.Fecha)
            .ToList();

        var problemEvents = eventosVigentes
            .Where(evento => IsProblemType(evento.TipoEventoNombre))
            .ToList();

        var problems = problemEvents
            .Select(evento => new
            {
                Tipo = evento.TipoEventoNombre ?? "Sin tipo",
                Potrero = asignacionesPorId.TryGetValue(evento.CampanaPotreroId, out var asignacion)
                    ? PotreroName(asignacion.Asignacion.PotreroId)
                    : "—",
                Evento = evento
            })
            .GroupBy(item => new { item.Tipo, item.Potrero })
            .Select(group => new ReportProblemRow(
                group.Key.Tipo,
                group.Key.Potrero,
                group.Count(),
                group.Max(item => (DateTime?)item.Evento.Fecha)))
            .OrderByDescending(row => row.Cantidad)
            .ToList();

        var eventAuthors = eventosFiltrados
            .Select(evento => new ReportEventAuthorRow(
                evento.Fecha,
                evento.TipoEventoNombre ?? "Sin tipo",
                asignacionesPorId.TryGetValue(evento.CampanaPotreroId, out var asignacion)
                    ? PotreroName(asignacion.Asignacion.PotreroId)
                    : "—",
                evento.RegistradoPor ?? "—",
                evento.Estado))
            .OrderByDescending(row => row.Fecha)
            .ToList();

        var taskResponses = tareasFiltradas
            .Where(tarea => tarea.EventoId.HasValue)
            .Select(tarea =>
            {
                var evento = eventosPorId.TryGetValue(tarea.EventoId!.Value, out var encontrado)
                    ? encontrado
                    : null;
                var descripcionEvento = evento is null
                    ? "Evento eliminado"
                    : $"{evento.TipoEventoNombre ?? "Evento"}: {evento.Descripcion ?? "—"}";

                return new ReportTaskResponseRow(
                    tarea.FechaPlanificada ?? tarea.FechaCreacion,
                    descripcionEvento,
                    tarea.TipoTareaNombre ?? "Tarea",
                    asignacionesPorId.TryGetValue(tarea.CampanaPotreroId, out var asignacion)
                        ? PotreroName(asignacion.Asignacion.PotreroId)
                        : "—",
                    tarea.Estado,
                    tarea.Resultado,
                    tarea.Costo ?? 0);
            })
            .OrderByDescending(row => row.Fecha)
            .ToList();

        var resources = tareasVigentes
            .SelectMany(tarea => tarea.Recursos)
            .Select(recurso => new
            {
                Recurso = recurso,
                Nombre = string.IsNullOrWhiteSpace(recurso.Nombre)
                    ? (recurso.Tipo == TipoRecursoTarea.Insumo ? "Insumo eliminado" : "Maquinaria eliminada")
                    : recurso.Nombre,
                Unidad = recurso.Tipo == TipoRecursoTarea.Insumo ? "—" : "hora"
            })
            .GroupBy(item => new { item.Recurso.Tipo, item.Nombre, item.Unidad })
            .Select(group => new ReportResourceRow(
                group.Key.Tipo == TipoRecursoTarea.Insumo ? "Insumo" : "Maquinaria",
                group.Key.Nombre,
                group.Key.Unidad,
                group.Sum(item => item.Recurso.Cantidad),
                group.Sum(item => item.Recurso.CostoTotal ?? 0)))
            .OrderByDescending(row => row.Costo)
            .ToList();

        var treatments = tareasVigentes
            .GroupBy(tarea => tarea.TipoTareaNombre ?? "Sin tipo")
            .Select(group =>
            {
                var total = group.Count();
                var resueltas = group.Count(tarea => tarea.SolucionoProblema == true);
                var costoPromedio = total == 0
                    ? 0
                    : group.Sum(tarea => tarea.Costo ?? 0) / total;

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
                var potreroId = asignacionesPorId.TryGetValue(evento.CampanaPotreroId, out var asignacion)
                    ? asignacion.Asignacion.PotreroId
                    : Guid.Empty;

                return new
                {
                    Establecimiento = EstablecimientoName(potreroId),
                    Potrero = PotreroName(potreroId),
                    Tipo = evento.TipoEventoNombre ?? "Sin tipo"
                };
            })
            .GroupBy(item => new { item.Establecimiento, item.Potrero })
            .Select(group => new ReportZoneRow(
                group.Key.Establecimiento,
                group.Key.Potrero,
                group.Count(),
                string.Join(
                    ", ",
                    group.GroupBy(item => item.Tipo)
                        .OrderByDescending(item => item.Count())
                        .Select(item => $"{item.Key} ({item.Count()})")
                        .Take(3))))
            .OrderByDescending(row => row.Eventos)
            .ToList();

        return new ReportDashboard
        {
            Campanas = campanasFiltradas.Count,
            CampanasActivas = campanasFiltradas.Count(campana => campana.Estado == EstadoCampana.Activa),
            CampanasCerradas = campanasFiltradas.Count(campana => campana.Estado == EstadoCampana.Cerrada),
            Eventos = eventosFiltrados.Count,
            EventosRegistrados = eventosFiltrados.Count(evento => evento.Estado == EstadoEvento.Registrado),
            EventosAnulados = eventosFiltrados.Count(evento => evento.Estado == EstadoEvento.Anulado),
            Tareas = tareasFiltradas.Count,
            TareasPendientes = tareasFiltradas.Count(tarea =>
                tarea.Estado is EstadoTarea.Planificada or EstadoTarea.Asignada),
            TareasRealizadas = tareasFiltradas.Count(tarea => tarea.Estado == EstadoTarea.Realizada),
            TareasVerificadas = tareasFiltradas.Count(tarea => tarea.Estado == EstadoTarea.Verificada),
            CostoTareas = tareasVigentes.Sum(tarea => tarea.Costo ?? 0),
            RecursosUtilizados = tareasVigentes.Sum(tarea => tarea.Recursos.Count),
            EventosPorTipo = eventosFiltrados
                .GroupBy(evento => evento.TipoEventoNombre ?? "Sin tipo")
                .Select(group => new ReportGroupRow(group.Key, group.Count()))
                .OrderByDescending(row => row.Cantidad)
                .ToList(),
            TareasPorTipo = tareasFiltradas
                .GroupBy(tarea => tarea.TipoTareaNombre ?? tarea.Estado.ToString())
                .Select(group => new ReportGroupRow(
                    group.Key,
                    group.Count(),
                    group.Where(tarea => tarea.Estado != EstadoTarea.Anulada)
                        .Sum(tarea => tarea.Costo ?? 0)))
                .OrderByDescending(row => row.Cantidad)
                .ToList(),
            CampanasDetalle = campaignRows,
            ActividadReciente = auditoria
                .Where(entrada =>
                    (!filtro.EmpresaId.HasValue || entrada.EmpresaId == filtro.EmpresaId) &&
                    IsWithin(SigaTime.ToLocal(entrada.FechaHoraUtc), filtro.Desde, filtro.Hasta))
                .OrderByDescending(entrada => entrada.FechaHoraUtc)
                .Take(20)
                .Select(entrada => new ReportActivityRow(
                    entrada.FechaHoraUtc,
                    entrada.Actor ?? "—",
                    entrada.Entidad,
                    entrada.Accion.ToString(),
                    entrada.EmpresaId))
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

        static bool IsProblemType(string? typeName) =>
            typeName is not null &&
            (typeName.Contains("plaga", StringComparison.OrdinalIgnoreCase) ||
             typeName.Contains("maleza", StringComparison.OrdinalIgnoreCase) ||
             typeName.Contains("enfermedad", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsWithin(DateTime value, DateTime? from, DateTime? to)
    {
        var date = value.Date;
        return (!from.HasValue || date >= from.Value.Date) &&
               (!to.HasValue || date <= to.Value.Date);
    }
}
