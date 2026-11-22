using System.Text.Json.Serialization;

namespace ObligatorioFrontEnd.Models.Geo;

/// <summary>
/// Coordenada de un establecimiento guardada en el frontend. La API todavía no
/// expone latitud/longitud para establecimientos, así que este overlay mantiene
/// la ubicación sin modificar el contrato ni la base de ApiSiga.
/// </summary>
public sealed class CoordenadaEstablecimiento
{
    public Guid EstablecimientoId { get; set; }

    public double Latitud { get; set; }

    public double Longitud { get; set; }

    public DateTime ActualizadaUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public bool EsValida =>
        double.IsFinite(Latitud)
        && double.IsFinite(Longitud)
        && Latitud is >= -90 and <= 90
        && Longitud is >= -180 and <= 180;
}

/// <summary>
/// Punto listo para dibujar. Se serializa a JSON y lo consume
/// <c>wwwroot/js/mapa.js</c>.
/// </summary>
public sealed record PuntoMapa(
    string Id,
    string Tipo,
    string Nombre,
    double Latitud,
    double Longitud,
    string? Detalle,
    string? Enlace);
