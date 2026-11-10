using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ObligatorioFrontEnd.Api;

/// <summary>
/// Error devuelto por la API. Hereda de <see cref="InvalidOperationException"/>
/// para que las páginas que ya muestran exception.Message sigan funcionando.
/// </summary>
public class ApiException : InvalidOperationException
{
    public ApiException(int status, string mensaje, IReadOnlyList<string>? errores = null)
        : base(mensaje)
    {
        Status = status;
        Errores = errores ?? [];
    }

    /// <summary>Código HTTP. 0 significa que no se pudo conectar con la API.</summary>
    public int Status { get; }

    public IReadOnlyList<string> Errores { get; }

    public static async Task<ApiException> DesdeRespuestaAsync(
        HttpResponseMessage response,
        JsonSerializerOptions json,
        CancellationToken cancellationToken)
    {
        ProblemaDto? problema = null;

        try
        {
            problema = await response.Content
                .ReadFromJsonAsync<ProblemaDto>(json, cancellationToken);
        }
        catch (JsonException)
        {
            // La respuesta no era problem+json; se usa el mensaje por defecto.
        }
        catch (NotSupportedException)
        {
            // Contenido vacío o tipo no soportado.
        }

        var errores = problema?.Errors is { } erroresApi
            ? erroresApi
                .SelectMany(e => e.Value)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList()
            : [];

        var mensaje = (int)response.StatusCode switch
        {
            (int)HttpStatusCode.Forbidden => "No tenés permisos para esta operación.",
            _ => problema?.Detail
                 ?? problema?.Title
                 ?? $"La API respondió con el código {(int)response.StatusCode}."
        };

        return new ApiException((int)response.StatusCode, mensaje, errores);
    }

    private sealed record ProblemaDto(
        string? Title,
        string? Detail,
        Dictionary<string, string[]>? Errors);
}
