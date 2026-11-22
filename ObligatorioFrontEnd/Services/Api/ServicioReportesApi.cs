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
    private readonly IUserScope _usuario;
    private readonly IServicioEmpresas _empresas;
    private readonly IServicioEstablecimientos _establecimientos;
    private readonly IServicioPotreros _potreros;
    private readonly IServicioCampanas _campanas;
    private readonly IServicioEventos _eventos;
    private readonly IServicioTareas _tareas;
    private readonly IServicioAuditoria _auditoria;

    public ServicioReportesApi(
        IUserScope usuario,
        IServicioEmpresas empresas,
        IServicioEstablecimientos establecimientos,
        IServicioPotreros potreros,
        IServicioCampanas campanas,
        IServicioEventos eventos,
        IServicioTareas tareas,
        IServicioAuditoria auditoria)
    {
        _usuario = usuario;
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
        CancellationToken cancellationToken = default,
        int? limiteAuditoria = null)
    {
        var empresasTask = CargarSiPuedeAsync(
            SigaModule.Empresas,
            () => _empresas.GetAllAsync(includeInactive: true, cancellationToken));
        var establecimientosTask = CargarSiPuedeAsync(
            SigaModule.Establecimientos,
            () => _establecimientos.GetAllAsync(incluirInactivos: true, cancellationToken));
        var potrerosTask = CargarSiPuedeAsync(
            SigaModule.Potreros,
            () => _potreros.GetAllAsync(incluirInactivos: true, cancellationToken));
        var campanasTask = CargarSiPuedeAsync(
            SigaModule.Campanas,
            () => _campanas.GetAllAsync(incluirAnuladas: true, cancellationToken));
        var eventosTask = CargarSiPuedeAsync(
            SigaModule.Eventos,
            () => _eventos.GetAllAsync(incluirAnulados: true, cancellationToken: cancellationToken));
        var tareasTask = CargarSiPuedeAsync(
            SigaModule.Tareas,
            () => _tareas.GetAllAsync(incluirAnuladas: true, cancellationToken: cancellationToken));
        var auditoriaTask = CargarSiPuedeAsync(
            SigaModule.Auditoria,
            () => CargarAuditoriaAsync(limiteAuditoria, cancellationToken));

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

    private async Task<IReadOnlyList<AuditoriaDto>> CargarAuditoriaAsync(
        int? limiteAuditoria,
        CancellationToken cancellationToken)
    {
        // El resumen solo muestra las últimas novedades: no hace falta recorrer
        // todas las páginas de auditoría.
        if (limiteAuditoria is int limite)
        {
            var pagina = await _auditoria.BuscarAsync(
                pagina: 1,
                tamanoPagina: limite,
                cancellationToken: cancellationToken);
            return pagina.Items;
        }

        return await _auditoria.BuscarTodoAsync(cancellationToken: cancellationToken);
    }

    private Task<IReadOnlyList<T>> CargarSiPuedeAsync<T>(
        SigaModule modulo,
        Func<Task<IReadOnlyList<T>>> cargar)
    {
        var usuario = _usuario.CurrentUser;

        // El rol manda: pedir un listado que la API va a rechazar solo genera 403.
        if (usuario is null || !Permissions.CanView(usuario.Role, modulo))
        {
            return Task.FromResult<IReadOnlyList<T>>([]);
        }

        return VaciarSiFallaAsync(cargar);
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
