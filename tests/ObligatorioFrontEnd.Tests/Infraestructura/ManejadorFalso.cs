using System.Net;
using System.Text;
using System.Text.Json;

namespace ObligatorioFrontEnd.Tests.Infraestructura;

/// <summary>Datos de una solicitud capturados antes de que el cliente la libere.</summary>
public sealed record SolicitudRegistrada(
    HttpMethod Metodo,
    string Ruta,
    string? Autorizacion,
    string? Cuerpo);

/// <summary>
/// Reemplazo de <see cref="HttpMessageHandler"/> para probar <c>ClienteApi</c>
/// sin levantar la API. Registra cada solicitud y delega la respuesta.
/// </summary>
public sealed class ManejadorFalso : HttpMessageHandler
{
    private readonly Func<SolicitudRegistrada, int, HttpResponseMessage> _responder;
    private int _cantidad;

    public ManejadorFalso(Func<SolicitudRegistrada, int, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public ManejadorFalso(Func<SolicitudRegistrada, HttpResponseMessage> responder)
        : this((solicitud, _) => responder(solicitud))
    {
    }

    public List<SolicitudRegistrada> Solicitudes { get; } = [];

    public int Cantidad => Solicitudes.Count;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var cuerpo = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var solicitud = new SolicitudRegistrada(
            request.Method,
            request.RequestUri?.PathAndQuery ?? string.Empty,
            request.Headers.Authorization?.ToString(),
            cuerpo);

        Solicitudes.Add(solicitud);

        var numero = Interlocked.Increment(ref _cantidad);
        return _responder(solicitud, numero);
    }
}

/// <summary>Constructores de respuestas HTTP usadas por las pruebas.</summary>
public static class Respuestas
{
    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    public static HttpResponseMessage Json<T>(HttpStatusCode status, T valor) =>
        Json(status, JsonSerializer.Serialize(valor, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

    public static HttpResponseMessage Problema(
        HttpStatusCode status,
        string title,
        string? detail = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var cuerpo = JsonSerializer.Serialize(new
        {
            title,
            status = (int)status,
            detail,
            errors
        });

        return new HttpResponseMessage(status)
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, "application/problem+json")
        };
    }

    public static HttpResponseMessage SinContenido(HttpStatusCode status) => new(status);

    public static HttpResponseMessage Binario(
        HttpStatusCode status,
        byte[] contenido,
        string tipo = "image/png") =>
        new(status)
        {
            Content = new ByteArrayContent(contenido) { Headers = { ContentType = new(tipo) } }
        };
}
