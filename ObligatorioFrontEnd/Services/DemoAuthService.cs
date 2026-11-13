using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Sesión de demostración usada mientras la API de identidad no está configurada.
/// Es el único origen de datos simulado que queda en el frontend.
/// </summary>
public sealed class DemoAuthService : IAuthService
{
    private const string DemoPassword = "siga2026";

    private readonly SesionPersistencia persistencia;

    private readonly List<DemoUsuario> usuarios =
    [
        new() { Id = SeedIds.UsuarioAdmin, Email = "admin@siga.demo", Nombre = "Lucía Pereira", Rol = UserRole.AdministradorGeneral },
        new() { Id = SeedIds.UsuarioEmpresa, Email = "empresa@siga.demo", Nombre = "Martín Silva", Rol = UserRole.AdministradorEmpresa, EmpresaId = SeedIds.AgroDelSur },
        new() { Id = SeedIds.UsuarioIngeniero, Email = "ingeniero@siga.demo", Nombre = "Valentina Costa", Rol = UserRole.IngenieroAgronomo, EmpresaId = SeedIds.AgroDelSur },
        new() { Id = SeedIds.UsuarioOperario, Email = "operario@siga.demo", Nombre = "Diego Rodríguez", Rol = UserRole.OperarioCampo, EmpresaId = SeedIds.AgroDelSur, EstablecimientoIds = [SeedIds.LaEsperanza] },
        new() { Id = SeedIds.UsuarioEmpresa2, Email = "empresa2@siga.demo", Nombre = "Sofía Méndez", Rol = UserRole.AdministradorEmpresa, EmpresaId = SeedIds.CamposVerdes },
        new() { Id = SeedIds.UsuarioContratista, Email = "contratista@siga.demo", Nombre = "Nicolás Torres", Rol = UserRole.Contratista, EmpresaId = SeedIds.AgroDelSur }
    ];

    public DemoAuthService(SesionPersistencia persistencia)
    {
        this.persistencia = persistencia;
    }

    public event Action? StateChanged;

    /// <summary>El mock no tiene tokens, así que nunca expira.</summary>
    public event Action? Expirada
    {
        add { }
        remove { }
    }

    public DemoUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public IReadOnlyList<DemoUser> Users => usuarios
        .Where(usuario => usuario.Activo)
        .Select(ToDemoUser)
        .ToList();

    public string ScopeLabel => CurrentUser?.ScopeLabel ?? string.Empty;

    public bool CanAccessEmpresa(Guid empresaId) => CurrentUser is { } user &&
        (user.EmpresaId is null || user.EmpresaId == empresaId);

    public bool CanAccessEstablecimiento(Guid establecimientoId, Guid empresaId) => CurrentUser is { } user &&
        CanAccessEmpresa(empresaId) &&
        (user.EmpresaId is null || user.EstablecimientoIds.Count == 0 || user.EstablecimientoIds.Contains(establecimientoId));

    public bool CanView(SigaModule module) => CurrentUser is { } user && Permissions.CanView(user.Role, module);

    public bool CanManage(SigaModule module) => CurrentUser is { } user && Permissions.CanManage(user.Role, module);

    public async Task<bool> LoginAsync(
        string email,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        if (!Login(email, contrasena))
        {
            return false;
        }

        await persistencia.GuardarAsync(new SesionGuardada { UsuarioDemo = CurrentUser });
        return true;
    }

    public bool Login(string email, string password)
    {
        var user = usuarios.FirstOrDefault(candidate =>
            candidate.Activo && candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (user is null || password != DemoPassword)
        {
            return false;
        }

        CurrentUser = ToDemoUser(user);
        StateChanged?.Invoke();
        return true;
    }

    public async Task<bool> RestaurarAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentUser is not null)
        {
            return true;
        }

        var sesion = await persistencia.RestaurarAsync();

        if (sesion?.UsuarioDemo is not { } usuario)
        {
            if (sesion is not null)
            {
                await persistencia.OlvidarAsync();
            }

            return false;
        }

        CurrentUser = usuario;
        StateChanged?.Invoke();
        return true;
    }

    public bool HasAnyRole(params UserRole[] roles) => CurrentUser is not null && roles.Contains(CurrentUser.Role);

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        await persistencia.OlvidarAsync();
        StateChanged?.Invoke();
    }

    private static DemoUser ToDemoUser(DemoUsuario user)
    {
        var empresaNombre = user.EmpresaId switch
        {
            null => "Toda la plataforma",
            var id when id == SeedIds.AgroDelSur => "Agro del Sur",
            var id when id == SeedIds.CamposVerdes => "Campos Verdes",
            _ => "Mi empresa"
        };

        var scope = user.EmpresaId is null
            ? "Toda la plataforma"
            : user.EstablecimientoIds.Count == 0
                ? empresaNombre
                : "Establecimiento La Esperanza";

        return new DemoUser(
            user.Id,
            user.Email,
            user.Nombre,
            user.Rol,
            user.EmpresaId,
            user.EstablecimientoIds,
            scope,
            empresaNombre);
    }

    private sealed class DemoUsuario
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public UserRole Rol { get; init; }
        public bool Activo { get; init; } = true;
        public Guid? EmpresaId { get; init; }
        public IReadOnlyList<Guid> EstablecimientoIds { get; init; } = [];
    }
}
