using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ObligatorioFrontEnd.Services;

namespace ObligatorioFrontEnd.Api;

/// <summary>
/// Transporte HTTP hacia ApiSiga. No conoce reglas de negocio: solo agrega el
/// token, deserializa y traduce los errores de la API a <see cref="ApiException"/>.
/// </summary>
public sealed class ClienteApi
{
    /// <summary>
    /// La API serializa enums como string y usa camelCase (defaults de ASP.NET Core).
    /// <see cref="JsonSerializerDefaults.Web"/> resuelve el camelCase y el converter
    /// los enums; sin ambos, la deserialización falla.
    /// </summary>
    public static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

    private readonly HttpClient _http;
    private readonly TokenStore _token;

    public ClienteApi(HttpClient http, TokenStore token)
    {
        _http = http;
        _token = token;
    }

    /// <summary>Dirección base de la API, usada para resolver URLs relativas de archivos.</summary>
    public Uri? BaseAddress => _http.BaseAddress;

    public Task<T> GetAsync<T>(string ruta, CancellationToken cancellationToken = default) =>
        EnviarJsonAsync<T>(HttpMethod.Get, ruta, null, cancellationToken);

    public Task<T> PostAsync<T>(string ruta, object cuerpo, CancellationToken cancellationToken = default) =>
        EnviarJsonAsync<T>(HttpMethod.Post, ruta, cuerpo, cancellationToken);

    public Task<T> PutAsync<T>(string ruta, object cuerpo, CancellationToken cancellationToken = default) =>
        EnviarJsonAsync<T>(HttpMethod.Put, ruta, cuerpo, cancellationToken);

    public Task DeleteAsync(string ruta, CancellationToken cancellationToken = default) =>
        EnviarSinRespuestaAsync(HttpMethod.Delete, ruta, null, cancellationToken);

    /// <summary>Para acciones que responden 204 sin cuerpo.</summary>
    public Task PostSinRespuestaAsync(
        string ruta,
        object cuerpo,
        CancellationToken cancellationToken = default) =>
        EnviarSinRespuestaAsync(HttpMethod.Post, ruta, cuerpo, cancellationToken);

    /// <summary>Para acciones que responden 204 sin cuerpo y no llevan payload.</summary>
    public Task PostSinRespuestaAsync(
        string ruta,
        CancellationToken cancellationToken = default) =>
        EnviarSinRespuestaAsync(HttpMethod.Post, ruta, null, cancellationToken);

    /// <summary>Subida de archivos con <c>multipart/form-data</c>.</summary>
    public async Task<T> PostMultipartAsync<T>(
        string ruta,
        MultipartFormDataContent contenido,
        CancellationToken cancellationToken = default)
    {
        using var request = CrearSolicitud(HttpMethod.Post, ruta);
        request.Content = contenido;
        using var response = await EjecutarAsync(request, cancellationToken);
        await ValidarAsync(response, cancellationToken);

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
                   ?? throw new ApiException(
                       (int)response.StatusCode,
                       "La API devolvió una respuesta vacía.");
        }
        catch (JsonException)
        {
            throw new ApiException(
                (int)response.StatusCode,
                "La API devolvió un formato inesperado al subir el archivo.");
        }
    }

    /// <summary>Descarga binaria autenticada (por ejemplo el contenido de una foto).</summary>
    public async Task<byte[]?> GetBytesAsync(
        string ruta,
        CancellationToken cancellationToken = default)
    {
        using var request = CrearSolicitud(HttpMethod.Get, ruta);
        using var response = await EjecutarAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ValidarAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<T> EnviarJsonAsync<T>(
        HttpMethod metodo,
        string ruta,
        object? cuerpo,
        CancellationToken cancellationToken)
    {
        using var request = CrearSolicitud(metodo, ruta);

        if (cuerpo is not null)
        {
            request.Content = JsonContent.Create(cuerpo, cuerpo.GetType(), options: Json);
        }

        using var response = await EjecutarAsync(request, cancellationToken);
        await ValidarAsync(response, cancellationToken);

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
                   ?? throw new ApiException(
                       (int)response.StatusCode,
                       "La API devolvió una respuesta vacía.");
        }
        catch (JsonException)
        {
            throw new ApiException(
                (int)response.StatusCode,
                "La API devolvió un formato inesperado. Revisá la configuración o la versión del servidor.");
        }
    }

    private async Task EnviarSinRespuestaAsync(
        HttpMethod metodo,
        string ruta,
        object? cuerpo,
        CancellationToken cancellationToken)
    {
        using var request = CrearSolicitud(metodo, ruta);

        if (cuerpo is not null)
        {
            request.Content = JsonContent.Create(cuerpo, cuerpo.GetType(), options: Json);
        }

        using var response = await EjecutarAsync(request, cancellationToken);
        await ValidarAsync(response, cancellationToken);
    }

    private HttpRequestMessage CrearSolicitud(HttpMethod metodo, string ruta)
    {
        var request = new HttpRequestMessage(metodo, ruta);

        if (_token.Token is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private async Task<HttpResponseMessage> EjecutarAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(
                0,
                "No se pudo conectar con el servidor. Verificá que la API esté en ejecución.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiException(0, "La API tardó demasiado en responder.");
        }
    }

    private async Task ValidarAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // 401 con token enviado = sesión vencida. 401 sin token = login fallido.
        if (response.StatusCode == HttpStatusCode.Unauthorized && _token.Token is not null)
        {
            _token.Expirar();
            throw new ApiException(401, "La sesión expiró. Iniciá sesión nuevamente.");
        }

        throw await ApiException.DesdeRespuestaAsync(response, Json, cancellationToken);
    }
}
