using System.Globalization;
using System.Net.Http.Headers;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>Fotografías contra ApiSiga. Los archivos viajan como multipart.</summary>
public sealed class ServicioFotografiasApi : IServicioFotografias
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioFotografiasApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<FotografiaDto>> SubirAsync(
        Guid eventoId,
        IReadOnlyList<FotoUpload> fotos,
        double? latitud = null,
        double? longitud = null,
        CancellationToken cancellationToken = default)
    {
        if (fotos.Count == 0)
        {
            return [];
        }

        var creadas = new List<FotografiaDto>();

        foreach (var foto in fotos)
        {
            using var formulario = new MultipartFormDataContent();

            var archivo = new ByteArrayContent(foto.Contenido);
            archivo.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(foto.ContentType)
                    ? "application/octet-stream"
                    : foto.ContentType);

            formulario.Add(archivo, "archivo", foto.NombreArchivo);
            formulario.Add(
                new StringContent(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
                "fechaCaptura");

            if (latitud is double lat)
            {
                formulario.Add(
                    new StringContent(lat.ToString(CultureInfo.InvariantCulture)),
                    "latitud");
            }

            if (longitud is double lng)
            {
                formulario.Add(
                    new StringContent(lng.ToString(CultureInfo.InvariantCulture)),
                    "longitud");
            }

            var creada = await _api.PostMultipartAsync<FotografiaDto>(
                $"/api/v1/eventos/{eventoId}/fotografias", formulario, cancellationToken);

            creadas.Add(creada);
        }

        _notificador.Notificar(ModulosDatos.Fotos);
        return creadas;
    }

    public async Task<FotografiaDto?> OcultarAsync(
        Guid eventoId,
        Guid fotoId,
        string motivo,
        long version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var oculta = await _api.PutAsync<FotografiaDto>(
                $"/api/v1/eventos/{eventoId}/fotografias/{fotoId}/ocultar",
                new OcultarFotografiaDto { Motivo = motivo, Version = version },
                cancellationToken);

            _notificador.Notificar(ModulosDatos.Fotos);
            return oculta;
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<string?> ObtenerDataUrlAsync(
        FotografiaDto foto,
        CancellationToken cancellationToken = default)
    {
        if (!foto.Visible)
        {
            return null;
        }

        var bytes = await _api.GetBytesAsync(
            $"/api/v1/eventos/{foto.EventoId}/fotografias/{foto.Id}/archivo",
            cancellationToken);

        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        var mime = MimeDesdeArchivo(foto.Archivo);
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string MimeDesdeArchivo(string archivo) =>
        Path.GetExtension(archivo).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            _ => "application/octet-stream"
        };
}
