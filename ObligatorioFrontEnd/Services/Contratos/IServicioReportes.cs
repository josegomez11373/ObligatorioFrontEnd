using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public interface IServicioReportes
{
    /// <summary>
    /// Descarga los listados necesarios y calcula el tablero en el cliente.
    /// No existe un endpoint de reportes en la API.
    /// </summary>
    Task<ReportDashboard> BuildAsync(
        ReportFilter filtro,
        CancellationToken cancellationToken = default);
}
