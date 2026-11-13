using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Construye el tablero de reportes descargando los listados y calculando en el
/// cliente. Si un endpoint no está disponible, ese conjunto queda vacío en vez
/// de romper la pantalla.
/// </summary>
public sealed class ServicioReportesApi : IServicioReportes
{
    private readonly IServicioEmpresas _empresas;
    private readonly IServicioEstablecimientos _establecimientos;
    private readonly IServicioPotreros _potreros;
    private readonly IServicioCampanas _campanas;
    private readonly IServicioEventos _eventos;
    private readonly IServicioTareas _tareas;
    private readonly IServicioAuditoria _auditoria;

    public ServicioReportesApi(
        IServicioEmpresas empresas,
        IServicioEstablecimientos establecimientos,
        IServicioPotreros potreros,
        IServicioCampanas campanas,
        IServicioEventos eventos,
        IServicioTareas tareas,
        IServicioAuditoria auditoria)
    {
        _empresas = empresas;
        _establecimientos = establecimientos;
        _potreros = potreros;
        _campanas = campanas;
        _eventos = eventos;
        _tareas = tareas;
        _auditoria = auditoria;
    }

    public async Task<ReportDashboard> BuildAsync(
        ReportFilter filtro,
        CancellationToken cancellationToken = default)
    {
        var empresasTask = VaciarSiFallaAsync(
            () => _empresas.GetAllAsync(includeInactive: true, cancellationToken));
        var establecimientosTask = VaciarSiFallaAsync(
            () => _establecimientos.GetAllAsync(incluirInactivos: true, cancellationToken));
        var potrerosTask = VaciarSiFallaAsync(
            () => _potreros.GetAllAsync(incluirInactivos: true, cancellationToken));
        var campanasTask = VaciarSiFallaAsync(
            () => _campanas.GetAllAsync(incluirAnuladas: true, cancellationToken));
        var eventosTask = VaciarSiFallaAsync(
            () => _eventos.GetAllAsync(incluirAnulados: true, cancellationToken: cancellationToken));
        var tareasTask = VaciarSiFallaAsync(
            () => _tareas.GetAllAsync(incluirAnuladas: true, cancellationToken: cancellationToken));
        var auditoriaTask = VaciarSiFallaAsync(
            () => _auditoria.BuscarTodoAsync(cancellationToken: cancellationToken));

        await Task.WhenAll(
            empresasTask,
            establecimientosTask,
            potrerosTask,
            campanasTask,
            eventosTask,
            tareasTask,
            auditoriaTask);

        return CalculoReportes.Build(
            filtro,
            await campanasTask,
            await potrerosTask,
            await empresasTask,
            await establecimientosTask,
            await eventosTask,
            await tareasTask,
            await auditoriaTask);
    }

    private static async Task<IReadOnlyList<T>> VaciarSiFallaAsync<T>(
        Func<Task<IReadOnlyList<T>>> cargar)
    {
        try
        {
            return await cargar();
        }
        catch (ApiException)
        {
            return [];
        }
    }
}
