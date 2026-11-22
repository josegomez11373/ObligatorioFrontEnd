namespace ObligatorioFrontEnd.Models.Geo;

/// <summary>Respuesta de Nominatim que devuelve <c>buscarDireccion</c> en mapa.js.</summary>
public sealed record DireccionEncontrada(double Latitud, double Longitud, string Direccion);
