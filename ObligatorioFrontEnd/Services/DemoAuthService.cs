using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Temporary client-side session used while the API and real identity provider are not available.
/// </summary>
public sealed class DemoAuthService : IUserScope
{
    private const string DemoPassword = "siga2026";

    private static readonly IReadOnlyList<DemoUser> DemoUsers =
    [
        new("admin@siga.demo", "Lucía Pereira", UserRole.AdministradorGeneral, null, [], "Toda la plataforma"),
        new("empresa@siga.demo", "Martín Silva", UserRole.AdministradorEmpresa, SeedIds.AgroDelSur, [], "Agro del Sur"),
        new("ingeniero@siga.demo", "Valentina Costa", UserRole.IngenieroAgronomo, SeedIds.AgroDelSur, [], "Agro del Sur"),
        new("operario@siga.demo", "Diego Rodríguez", UserRole.OperarioCampo, SeedIds.AgroDelSur, [SeedIds.LaEsperanza], "Establecimiento La Esperanza"),
        new("empresa2@siga.demo", "Sofía Méndez", UserRole.AdministradorEmpresa, SeedIds.CamposVerdes, [], "Campos Verdes"),
        new("contratista@siga.demo", "Nicolás Torres", UserRole.Contratista, SeedIds.AgroDelSur, [], "Agro del Sur")
    ];

    public event Action? StateChanged;

    public DemoUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public IReadOnlyList<DemoUser> Users => DemoUsers;

    public string ScopeLabel => CurrentUser?.ScopeLabel ?? string.Empty;

    public bool CanAccessEmpresa(Guid empresaId) => CurrentUser is { } user &&
        (user.EmpresaId is null || user.EmpresaId == empresaId);

    public bool CanAccessEstablecimiento(Guid establecimientoId, Guid empresaId) => CurrentUser is { } user &&
        CanAccessEmpresa(empresaId) &&
        (user.EmpresaId is null || user.EstablecimientoIds.Count == 0 || user.EstablecimientoIds.Contains(establecimientoId));

    public bool CanView(SigaModule module) => CurrentUser is { } user && Permissions.CanView(user.Role, module);

    public bool CanManage(SigaModule module) => CurrentUser is { } user && Permissions.CanManage(user.Role, module);

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
