namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Guarda el JWT del circuito. Vive en el servidor Blazor: el token nunca
/// se envía al navegador. Está separado de <see cref="SesionService"/> para
/// que <c>ClienteApi</c> pueda leerlo sin crear una dependencia circular.
/// </summary>
public sealed class TokenStore
{
    public string? Token { get; private set; }

    public event Action? Expirada;

    public void Guardar(string token) => Token = token;

    public void Limpiar() => Token = null;

    public void Expirar()
    {
        Token = null;
        Expirada?.Invoke();
    }
}
