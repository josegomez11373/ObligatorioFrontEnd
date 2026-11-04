using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Temporary client-side session used while the API and real identity provider are not available.
/// </summary>
public sealed class DemoAuthService : IUserScope
{
    private const string DemoPassword = "siga2026";

    private readonly List<Usuario> usuarios =
    [
        new() { Id = SeedIds.UsuarioAdmin, Email = "admin@siga.demo", Nombre = "Lucía Pereira", Rol = UserRole.AdministradorGeneral },
        new() { Id = SeedIds.UsuarioEmpresa, Email = "empresa@siga.demo", Nombre = "Martín Silva", Rol = UserRole.AdministradorEmpresa, EmpresaId = SeedIds.AgroDelSur },
        new() { Id = SeedIds.UsuarioIngeniero, Email = "ingeniero@siga.demo", Nombre = "Valentina Costa", Rol = UserRole.IngenieroAgronomo, EmpresaId = SeedIds.AgroDelSur },
        new() { Id = SeedIds.UsuarioOperario, Email = "operario@siga.demo", Nombre = "Diego Rodríguez", Rol = UserRole.OperarioCampo, EmpresaId = SeedIds.AgroDelSur, EstablecimientoIds = [SeedIds.LaEsperanza] },
        new() { Id = SeedIds.UsuarioEmpresa2, Email = "empresa2@siga.demo", Nombre = "Sofía Méndez", Rol = UserRole.AdministradorEmpresa, EmpresaId = SeedIds.CamposVerdes },
        new() { Id = SeedIds.UsuarioContratista, Email = "contratista@siga.demo", Nombre = "Nicolás Torres", Rol = UserRole.Contratista, EmpresaId = SeedIds.AgroDelSur }
    ];

    public event Action? StateChanged;

    public DemoUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public IReadOnlyList<DemoUser> Users => usuarios.Where(u => u.Activo).Select(ToDemoUser).ToList();
    public IReadOnlyList<Usuario> UserRecords => usuarios;

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
        var user = usuarios.FirstOrDefault(candidate => candidate.Activo && candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user is null || password != DemoPassword)
        {
            return false;
        }

        CurrentUser = ToDemoUser(user);
        StateChanged?.Invoke();
        return true;
    }

    public bool HasAnyRole(params UserRole[] roles) => CurrentUser is not null && roles.Contains(CurrentUser.Role);

    public void Logout()
    {
        CurrentUser = null;
        StateChanged?.Invoke();
    }

    internal void AddUser(Usuario user) => usuarios.Add(user);

    internal bool ReplaceUser(Usuario user)
    {
        var index = usuarios.FindIndex(candidate => candidate.Id == user.Id);
        if (index < 0) return false;
        usuarios[index] = user;
        if (CurrentUser is { } current && current.Id == user.Id)
        {
            CurrentUser = ToDemoUser(user);
        }
        return true;
    }

    internal Usuario? FindById(Guid id) => usuarios.SingleOrDefault(user => user.Id == id);

    private static DemoUser ToDemoUser(Usuario user)
    {
        var scope = user.EmpresaId is null
            ? "Toda la plataforma"
            : user.EstablecimientoIds.Count == 0
                ? (user.EmpresaId == SeedIds.AgroDelSur ? "Agro del Sur" : "Campos Verdes")
                : "Establecimiento La Esperanza";
        return new DemoUser(user.Id, user.Email, user.Nombre, user.Rol, user.EmpresaId, user.EstablecimientoIds, scope);
    }
}
