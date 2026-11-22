using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;
using ObligatorioFrontEnd.Services;
using ObligatorioFrontEnd.Tests.Infraestructura;

namespace ObligatorioFrontEnd.Tests;

/// <summary>
/// El administrador general puede cambiar la empresa activa; el resto queda
/// fijado a la empresa del token. También se prueba que no se pueda seleccionar
/// una empresa inactiva o ajena a la lista disponible.
/// </summary>
public sealed class ContextoEmpresaTests
{
    private static readonly Guid EmpresaAlfa = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EmpresaZeta = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid EmpresaInactiva = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task AdministradorGeneral_CargaEmpresasActivasYSeleccionaLaPrimera()
    {
        var (contexto, auth, empresas) = Crear();
        auth.CurrentUser = UsuarioAdminGeneral();
        empresas.Empresas.AddRange([
            Empresa(EmpresaZeta, "Zeta", activa: true),
            Empresa(EmpresaAlfa, "Alfa", activa: true),
            Empresa(EmpresaInactiva, "Inactiva", activa: false)
        ]);

        var cambios = 0;
        contexto.Cambiada += () => cambios++;

        await contexto.InicializarAsync();

        Assert.True(contexto.PuedeCambiar);
        Assert.Equal(2, contexto.Disponibles.Count);
        Assert.Equal(new[] { "Alfa", "Zeta" }, contexto.Disponibles.Select(empresa => empresa.Nombre));
        Assert.Equal(EmpresaAlfa, contexto.EmpresaActivaId);
        Assert.True(cambios > 0);
    }

    [Fact]
    public async Task AdministradorGeneral_PuedeCambiarLaEmpresaActiva()
    {
        var (contexto, auth, empresas) = Crear();
        auth.CurrentUser = UsuarioAdminGeneral();
        empresas.Empresas.AddRange([
            Empresa(EmpresaAlfa, "Alfa", activa: true),
            Empresa(EmpresaZeta, "Zeta", activa: true)
        ]);

        await contexto.InicializarAsync();

        var cambio = await contexto.SeleccionarAsync(EmpresaZeta);

        Assert.True(cambio);
        Assert.Equal(EmpresaZeta, contexto.EmpresaActivaId);
        Assert.Equal("Zeta", contexto.EmpresaActiva!.Nombre);
    }

    [Fact]
    public async Task AdministradorGeneral_NoPuedeSeleccionarUnaEmpresaInactiva()
    {
        var (contexto, auth, empresas) = Crear();
        auth.CurrentUser = UsuarioAdminGeneral();
        empresas.Empresas.AddRange([
            Empresa(EmpresaAlfa, "Alfa", activa: true),
            Empresa(EmpresaInactiva, "Inactiva", activa: false)
        ]);

        await contexto.InicializarAsync();

        var cambio = await contexto.SeleccionarAsync(EmpresaInactiva);

        Assert.False(cambio);
        Assert.Equal(EmpresaAlfa, contexto.EmpresaActivaId);
    }

    [Fact]
    public async Task AdministradorGeneral_NoPuedeSeleccionarUnaEmpresaFueraDeLaLista()
    {
        var (contexto, auth, empresas) = Crear();
        auth.CurrentUser = UsuarioAdminGeneral();
        empresas.Empresas.Add(Empresa(EmpresaAlfa, "Alfa", activa: true));

        await contexto.InicializarAsync();

        var cambio = await contexto.SeleccionarAsync(Guid.NewGuid());

        Assert.False(cambio);
        Assert.Equal(EmpresaAlfa, contexto.EmpresaActivaId);
    }

    [Fact]
    public async Task AdministradorDeEmpresa_TieneLaEmpresaFijaDelToken()
    {
        var (contexto, auth, empresas) = Crear();
        auth.CurrentUser = UsuarioEmpresa(EmpresaAlfa, "Agro del Sur");
        empresas.Empresas.AddRange([
            Empresa(EmpresaAlfa, "Agro del Sur", activa: true),
            Empresa(EmpresaZeta, "Campos Verdes", activa: true)
        ]);

        await contexto.InicializarAsync();

        Assert.False(contexto.PuedeCambiar);
        Assert.Single(contexto.Disponibles);
        Assert.Equal(EmpresaAlfa, contexto.Disponibles[0].Id);
        Assert.Equal("Agro del Sur", contexto.Disponibles[0].Nombre);
        Assert.Equal(EmpresaAlfa, contexto.EmpresaActivaId);

        var cambio = await contexto.SeleccionarAsync(EmpresaZeta);
        Assert.False(cambio);
        Assert.Equal(EmpresaAlfa, contexto.EmpresaActivaId);
    }

    [Fact]
    public async Task Inicializar_SinUsuario_LimpiaElContexto()
    {
        var (contexto, auth, empresas) = Crear();
        auth.CurrentUser = UsuarioAdminGeneral();
        empresas.Empresas.Add(Empresa(EmpresaAlfa, "Alfa", activa: true));
        await contexto.InicializarAsync();

        auth.CurrentUser = null;
        await contexto.InicializarAsync();

        Assert.Null(contexto.EmpresaActivaId);
        Assert.Empty(contexto.Disponibles);
        Assert.False(contexto.PuedeCambiar);
    }

    [Fact]
    public async Task Limpiar_DejaElContextoVacio()
    {
        var (contexto, auth, empresas) = Crear();
        auth.CurrentUser = UsuarioAdminGeneral();
        empresas.Empresas.Add(Empresa(EmpresaAlfa, "Alfa", activa: true));
        await contexto.InicializarAsync();

        contexto.Limpiar();

        Assert.Null(contexto.EmpresaActivaId);
        Assert.Empty(contexto.Disponibles);
    }

    [Fact]
    public async Task NotificacionDeEmpresas_RefrescaElSelector()
    {
        var (contexto, auth, empresas, notificador) = CrearConNotificador();
        auth.CurrentUser = UsuarioAdminGeneral();
        empresas.Empresas.Add(Empresa(EmpresaAlfa, "Alfa", activa: true));
        await contexto.InicializarAsync();

        empresas.Empresas.Add(Empresa(EmpresaZeta, "Zeta", activa: true));
        notificador.Notificar(ModulosDatos.Empresas);
        await Task.Delay(50);

        Assert.Equal(2, contexto.Disponibles.Count);
    }

    private static (ContextoEmpresa Contexto, AuthFalsa Auth, EmpresasFalsas Empresas) Crear()
    {
        var (contexto, auth, empresas, _) = CrearConNotificador();
        return (contexto, auth, empresas);
    }

    private static (ContextoEmpresa Contexto, AuthFalsa Auth, EmpresasFalsas Empresas, NotificadorDatos Notificador)
        CrearConNotificador()
    {
        var auth = new AuthFalsa();
        var empresas = new EmpresasFalsas();
        var notificador = new NotificadorDatos();
        var storage = AlmacenamientoFalso.Crear();
        var contexto = new ContextoEmpresa(auth, empresas, notificador, storage);

        return (contexto, auth, empresas, notificador);
    }

    private static DemoUser UsuarioAdminGeneral() =>
        new(
            Guid.NewGuid(),
            "admin@siga.demo",
            "Administrador general",
            UserRole.AdministradorGeneral,
            null,
            [],
            "Toda la plataforma");

    private static DemoUser UsuarioEmpresa(Guid empresaId, string nombre) =>
        new(
            Guid.NewGuid(),
            "empresa@siga.demo",
            "Administrador de empresa",
            UserRole.AdministradorEmpresa,
            empresaId,
            [],
            nombre,
            nombre);

    private static EmpresaDto Empresa(Guid id, string nombre, bool activa) =>
        new(id, nombre, null, activa, new DateTime(2026, 1, 1), 1);

    private sealed class AuthFalsa : IAuthService
    {
        public DemoUser? CurrentUser { get; set; }

        public string ScopeLabel => CurrentUser?.ScopeLabel ?? string.Empty;

        public bool IsAuthenticated => CurrentUser is not null;

        public event Action? StateChanged
        {
            add { }
            remove { }
        }

        public event Action? Expirada
        {
            add { }
            remove { }
        }

        public bool CanAccessEmpresa(Guid empresaId) =>
            CurrentUser is { } usuario &&
            (usuario.EmpresaId is null || usuario.EmpresaId == empresaId);

        public Task<bool> LoginAsync(
            string email,
            string contrasena,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> RestaurarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task LogoutAsync() => Task.CompletedTask;

        public bool CanView(SigaModule modulo) =>
            CurrentUser is { } usuario && Permissions.CanView(usuario.Role, modulo);

        public bool CanManage(SigaModule modulo) =>
            CurrentUser is { } usuario && Permissions.CanManage(usuario.Role, modulo);
    }

    private sealed class EmpresasFalsas : IServicioEmpresas
    {
        public List<EmpresaDto> Empresas { get; } = [];

        public Task<IReadOnlyList<EmpresaDto>> GetAllAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmpresaDto>>(
                Empresas.Where(empresa => includeInactive || empresa.Activo).ToList());

        public Task<EmpresaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Empresas.FirstOrDefault(empresa => empresa.Id == id));

        public Task<EmpresaDto> CreateAsync(
            string nombre,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            Guid id,
            string nombre,
            bool activo,
            long expectedVersion = 0,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
