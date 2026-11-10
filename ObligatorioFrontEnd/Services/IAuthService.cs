using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Punto de acceso a la sesión para las páginas. Lo implementan tanto el
/// mock (<see cref="DemoAuthService"/>) como la API (<see cref="SesionService"/>).
/// </summary>
public interface IAuthService : IUserScope
{
    bool IsAuthenticated { get; }

    event Action? StateChanged;

    /// <summary>Se dispara cuando la API responde 401 con un token ya enviado.</summary>
    event Action? Expirada;

    Task<bool> LoginAsync(string email, string contrasena, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera la sesión guardada por el navegador (por ejemplo tras un F5).
    /// Debe llamarse una sola vez, cuando el circuito interactivo ya está listo.
    /// </summary>
    Task<bool> RestaurarAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync();

    bool CanView(SigaModule modulo);

    bool CanManage(SigaModule modulo);
}
