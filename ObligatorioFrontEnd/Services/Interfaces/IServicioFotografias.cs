using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public interface IServicioFotografias
{
    /// <summary>
    /// Sube cada archivo por separado: la API recibe una fotografía por request
    /// (<c>POST /api/v1/eventos/{id}/fotografias</c>).
    /// </summary>
    Task<IReadOnlyList<FotografiaDto>> SubirAsync(
        Guid eventoId,
        IReadOnlyList<FotoUpload> fotos,
        double? latitud = null,
        double? longitud = null,
        CancellationToken cancellationToken = default);

    Task<FotografiaDto?> OcultarAsync(
        Guid eventoId,
        Guid fotoId,
        string motivo,
        long version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Descarga autenticada del archivo y la convierte en <c>data:</c> URL.
    /// Un <c>&lt;img src&gt;</c> directo no puede enviar el bearer token.
    /// </summary>
    Task<string?> ObtenerDataUrlAsync(
        FotografiaDto foto,
        CancellationToken cancellationToken = default);
}
