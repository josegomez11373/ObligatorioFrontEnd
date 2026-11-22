using Microsoft.JSInterop;

namespace ObligatorioFrontEnd.Tests.Infraestructura;

/// <summary>
/// En pruebas no hay navegador. Los servicios que persisten sesión capturan la
/// excepción y continúan, así que este runtime solo debe fallar.
/// </summary>
public sealed class JsRuntimeFalso : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        throw new InvalidOperationException("JavaScript no está disponible en las pruebas.");

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args) =>
        throw new InvalidOperationException("JavaScript no está disponible en las pruebas.");
}
