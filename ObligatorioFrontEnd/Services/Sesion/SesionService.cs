using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Sesión real contra ApiSiga. Vive en el servidor Blazor (scoped = circuito):
/// el JWT nunca se expone al navegador.
/// </summary>
public sealed class SesionService : IAuthService
{
    private readonly ClienteApi _api;
    private readonly TokenStore _token;
    private readonly SesionPersistencia _persistencia;

    public SesionService(ClienteApi api, TokenStore token, SesionPersistencia persistencia)
    {
        _api = api;
        _token = token;
        _persistencia = persistencia;

        _token.Expirada += () =>
        {
            CurrentUser = null;
            _persistencia.OlvidarLocal();
            Expirada?.Invoke();
            StateChanged?.Invoke();
        };
    }

    public DemoUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public string ScopeLabel => CurrentUser?.ScopeLabel ?? string.Empty;

    public event Action? StateChanged;
    public event Action? Expirada;

    public bool CanAccessEmpresa(Guid empresaId) =>
        CurrentUser is { } user &&
        (user.EmpresaId is null || user.EmpresaId == empresaId);

    public bool CanAccessEstablecimiento(Guid establecimientoId, Guid empresaId) =>
        CurrentUser is { } user &&
        CanAccessEmpresa(empresaId) &&
        (user.EmpresaId is null ||
         user.EstablecimientoIds.Count == 0 ||
         user.EstablecimientoIds.Contains(establecimientoId));

    public bool CanView(SigaModule modulo) =>
        CurrentUser is { } user && Permissions.CanView(user.Role, modulo);

    public bool CanManage(SigaModule modulo) =>
        CurrentUser is { } user && Permissions.CanManage(user.Role, modulo);

    public async Task<bool> LoginAsync(
        string email,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        RespuestaInicioSesionDto respuesta;

        try
        {
            respuesta = await _api.PostAsync<RespuestaInicioSesionDto>(
                "/api/v1/auth/login",
                new IniciarSesionDto { Email = email, Contrasena = contrasena },
                cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 401)
        {
            return false;
        }

        _token.Guardar(respuesta.Token);
        CurrentUser = AModelo(respuesta.Usuario);

        await _persistencia.GuardarAsync(new SesionGuardada
        {
            Token = respuesta.Token,
            Expira = respuesta.Expira,
            Usuario = respuesta.Usuario
        });

        StateChanged?.Invoke();
        return true;
    }

    public async Task<bool> RestaurarAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentUser is not null)
        {
            return true;
        }

        var sesion = await _persistencia.RestaurarAsync();

        if (sesion?.Token is null || sesion.Usuario is null)
        {
            if (sesion is not null)
            {
                await _persistencia.OlvidarAsync();
            }

            return false;
        }

        if (sesion.Expira != default && sesion.Expira.ToUniversalTime() <= DateTime.UtcNow)
        {
            await _persistencia.OlvidarAsync();
            return false;
        }

        _token.Guardar(sesion.Token);
        CurrentUser = AModelo(sesion.Usuario);
        StateChanged?.Invoke();
        return true;
    }

    public async Task LogoutAsync()
    {
        _token.Limpiar();
        CurrentUser = null;
        await _persistencia.OlvidarAsync();
        StateChanged?.Invoke();
    }

    private static DemoUser AModelo(UsuarioSesionDto usuario) =>
        new(
            usuario.Id,
            usuario.Email,
            usuario.Nombre,
            AModelo(usuario.Rol),
            usuario.EmpresaId,
            usuario.EstablecimientoIds,
            usuario.EtiquetaAlcance,
            usuario.EmpresaNombre);

    private static UserRole AModelo(RolApi rol) => rol switch
    {
        RolApi.AdministradorGeneral => UserRole.AdministradorGeneral,
        RolApi.AdministradorEmpresa => UserRole.AdministradorEmpresa,
        RolApi.IngenieroAgronomo => UserRole.IngenieroAgronomo,
        RolApi.OperarioCampo => UserRole.OperarioCampo,
        RolApi.Contratista => UserRole.Contratista,
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, "Rol desconocido.")
    };
}
