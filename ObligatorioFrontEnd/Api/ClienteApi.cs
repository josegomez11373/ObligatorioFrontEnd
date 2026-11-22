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
/// Los GET se reintentan y se cachean por circuito para sobrevivir al arranque
/// en frío de la API y evitar listados duplicados.
/// </summary>
public sealed class ClienteApi
{
    private const int IntentosGet = 3;

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
    private readonly CacheDatos _cache;

    public ClienteApi(HttpClient http, TokenStore token, CacheDatos cache)
    {
        _http = http;
        _token = token;
        _cache = cache;
    }

    /// <summary>Dirección base de la API, usada para resolver URLs relativas de archivos.</summary>
    public Uri? BaseAddress => _http.BaseAddress;

    public async Task<T> GetAsync<T>(string ruta, CancellationToken cancellationToken = default)
    {
        if (_cache.TryObtener<T>(ruta, out var cacheado))
        {
            return cacheado;
        }

        var valor = await EnviarJsonAsync<T>(HttpMethod.Get, ruta, null, cancellationToken);
        _cache.Guardar(ruta, valor);
        return valor;
    }

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
        using var response = await _http.SendAsync(request, cancellationToken);
        await ValidarAsync(response, cancellationToken);
        _cache.Limpiar();

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
        using var response = await EnviarAsync(request, cancellationToken);

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
        var intentos = metodo == HttpMethod.Get ? IntentosGet : 1;

        for (var intento = 1; ; intento++)
        {
            using var request = CrearSolicitud(metodo, ruta);

            if (cuerpo is not null)
            {
                request.Content = JsonContent.Create(cuerpo, cuerpo.GetType(), options: Json);
            }

            try
            {
                using var response = await _http.SendAsync(request, cancellationToken);
                await ValidarAsync(response, cancellationToken);

                if (metodo != HttpMethod.Get)
                {
                    _cache.Limpiar();
                }

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
            catch (ApiException ex) when (intento < intentos && EsTransitorio(ex.Status))
            {
                await EsperarAsync(intento, cancellationToken);
            }
            catch (HttpRequestException) when (intento < intentos)
            {
                await EsperarAsync(intento, cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw new ApiException(
                    0,
                    "No se pudo conectar con el servidor. Verificá que la API esté en ejecución.");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ApiException(
                    0,
                    "La API tardó demasiado en responder. Si acaba de iniciar, esperá unos segundos y reintentá.");
            }
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

        using var response = await EnviarAsync(request, cancellationToken);
        await ValidarAsync(response, cancellationToken);
        _cache.Limpiar();
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

    /// <summary>
    /// Envía sin reintentos. Solo traduce los fallos de transporte; los códigos
    /// HTTP los evalúa <see cref="ValidarAsync"/>.
    /// </summary>
    private async Task<HttpResponseMessage> EnviarAsync(
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

    private static bool EsTransitorio(int status) =>
        status is (int)HttpStatusCode.BadGateway
            or (int)HttpStatusCode.ServiceUnavailable
            or (int)HttpStatusCode.GatewayTimeout;

    private static Task EsperarAsync(int intento, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(400 * intento), cancellationToken);
}
