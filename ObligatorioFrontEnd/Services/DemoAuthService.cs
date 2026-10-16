using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Temporary client-side session used while the API and real identity provider are not available.
/// </summary>
public sealed class DemoAuthService
{
    private const string DemoPassword = "siga2026";

    private static readonly IReadOnlyList<DemoUser> DemoUsers =
    [
        new("admin@siga.demo", "Lucía Pereira", UserRole.AdministradorGeneral, "Toda la plataforma"),
        new("empresa@siga.demo", "Martín Silva", UserRole.AdministradorEmpresa, "Agro del Sur"),
        new("ingeniero@siga.demo", "Valentina Costa", UserRole.IngenieroAgronomo, "Agro del Sur"),
        new("operario@siga.demo", "Diego Rodríguez", UserRole.OperarioCampo, "Establecimiento La Esperanza")
    ];

    public event Action? StateChanged;

    public DemoUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public IReadOnlyList<DemoUser> Users => DemoUsers;

    public bool Login(string email, string password)
    {
        var user = DemoUsers.SingleOrDefault(candidate => candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user is null || password != DemoPassword)
        {
            return false;
        }

        CurrentUser = user;
        StateChanged?.Invoke();
        return true;
    }

    public bool HasAnyRole(params UserRole[] roles) => CurrentUser is not null && roles.Contains(CurrentUser.Role);

    public void Logout()
    {
        CurrentUser = null;
        StateChanged?.Invoke();
    }
}
