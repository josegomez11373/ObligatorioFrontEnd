using ObligatorioFrontEnd.Models.Geo;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Coordenadas de establecimientos propias del frontend. ApiSiga no las expone,
/// así que se guardan en <c>Data/establecimientos-geo.json</c>.
/// </summary>
public interface IServicioGeoEstablecimientos
{
    Task<IReadOnlyList<CoordenadaEstablecimiento>> GetTodasAsync(
        CancellationToken cancellationToken = default);

    Task<CoordenadaEstablecimiento> GuardarAsync(
        Guid establecimientoId,
        double latitud,
        double longitud,
        CancellationToken cancellationToken = default);

    Task<bool> EliminarAsync(
        Guid establecimientoId,
        CancellationToken cancellationToken = default);
}
