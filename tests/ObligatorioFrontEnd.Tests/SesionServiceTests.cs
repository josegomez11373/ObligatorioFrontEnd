using System.Net;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;
using ObligatorioFrontEnd.Services;
using ObligatorioFrontEnd.Tests.Infraestructura;

namespace ObligatorioFrontEnd.Tests;

/// <summary>
/// Pruebas de integración del login y del alcance por empresa. Usan un
/// <see cref="HttpMessageHandler"/> falso, sin levantar ApiSiga.
/// </summary>
public sealed class SesionServiceTests
{
    [Fact]
    public async Task LoginAsync_ConCredencialesValidas_CreaLaSesionYGuardaElToken()
    {
        var (sesion, token, entorno) = CrearSesion(_ => RespuestaLogin());

        var ok = await sesion.LoginAsync("admin@siga.demo", "siga2026");

        Assert.True(ok);
        Assert.True(sesion.IsAuthenticated);
        Assert.Equal(UserRole.AdministradorGeneral, sesion.CurrentUser!.Role);
        Assert.Equal("jwt-de-prueba", token.Token);

        var solicitud = Assert.Single(entorno.Manejador.Solicitudes);
        Assert.Equal(HttpMethod.Post, solicitud.Metodo);
        Assert.Equal("/api/v1/auth/login", solicitud.Ruta);
        Assert.Contains("\"email\":\"admin@siga.demo\"", solicitud.Cuerpo);
        Assert.Contains("\"contrasena\":\"siga2026\"", solicitud.Cuerpo);
    }

    [Fact]
    public async Task LoginAsync_Con401_DevuelveFalseSinCrearSesion()
    {
        var (sesion, token, _) = CrearSesion(_ =>
            Respuestas.Problema(HttpStatusCode.Unauthorized, "No autorizado", "Credenciales inválidas."));

        var ok = await sesion.LoginAsync("admin@siga.demo", "incorrecta");

        Assert.False(ok);
        Assert.False(sesion.IsAuthenticated);
        Assert.Null(sesion.CurrentUser);
        Assert.Null(token.Token);
    }

    [Fact]
    public async Task CanAccessEmpresa_AdministradorGeneral_AccedeATodasLasEmpresas()
    {
        var (sesion, _, _) = CrearSesion(_ => RespuestaLogin(empresaId: null));

        await sesion.LoginAsync("admin@siga.demo", "siga2026");

        Assert.True(sesion.CanAccessEmpresa(Guid.NewGuid()));
        Assert.True(sesion.CanAccessEmpresa(Guid.NewGuid()));
    }

    [Fact]
    public async Task CanAccessEmpresa_AdministradorDeEmpresa_SoloAccedeASuEmpresa()
    {
        var propia = Guid.NewGuid();
        var (sesion, _, _) = CrearSesion(_ => RespuestaLogin(
            empresaId: propia,
            rol: "AdministradorEmpresa",
            etiqueta: "Agro del Sur"));

        await sesion.LoginAsync("empresa@siga.demo", "siga2026");

        Assert.True(sesion.CanAccessEmpresa(propia));
        Assert.False(sesion.CanAccessEmpresa(Guid.NewGuid()));
    }

    [Fact]
    public void CanAccessEmpresa_SinSesion_EsFalse()
    {
        var (sesion, _, _) = CrearSesion(_ => RespuestaLogin());

        Assert.False(sesion.CanAccessEmpresa(Guid.NewGuid()));
    }

    [Fact]
    public async Task CanViewYCanManage_DeleganEnLaMatrizDePermisos()
    {
        var (sesion, _, _) = CrearSesion(_ => RespuestaLogin(
            rol: "Contratista",
            etiqueta: "Agro del Sur · tareas asignadas"));

        await sesion.LoginAsync("contratista@siga.demo", "siga2026");

        Assert.True(sesion.CanView(SigaModule.Tareas));
        Assert.True(sesion.CanView(SigaModule.Historial));
        Assert.False(sesion.CanView(SigaModule.Empresas));
        Assert.False(sesion.CanView(SigaModule.Eventos));
        Assert.False(sesion.CanManage(SigaModule.Eventos));
    }

    [Fact]
    public async Task LogoutAsync_LimpiaLaSesionYElToken()
    {
        var (sesion, token, _) = CrearSesion(_ => RespuestaLogin());
        await sesion.LoginAsync("admin@siga.demo", "siga2026");

        await sesion.LogoutAsync();

        Assert.False(sesion.IsAuthenticated);
        Assert.Null(sesion.CurrentUser);
        Assert.Null(token.Token);
    }

    [Fact]
    public async Task TokenExpirado_LimpiaLaSesionYDisparaElEvento()
    {
        var primera = true;
        var (sesion, token, entorno) = CrearSesion(_ =>
        {
            if (primera)
            {
                primera = false;
                return RespuestaLogin();
            }

            return Respuestas.Problema(HttpStatusCode.Unauthorized, "No autorizado", "Token vencido.");
        });

        await sesion.LoginAsync("admin@siga.demo", "siga2026");

        var expirada = false;
        sesion.Expirada += () => expirada = true;

        await Assert.ThrowsAsync<ApiException>(
            () => entorno.Api.GetAsync<List<EmpresaDto>>("/api/v1/empresas"));

        Assert.True(expirada);
        Assert.False(sesion.IsAuthenticated);
        Assert.Null(sesion.CurrentUser);
        Assert.Null(token.Token);
    }

    [Fact]
    public async Task RestaurarAsync_SinSesionGuardada_DevuelveFalse()
    {
        var (sesion, _, _) = CrearSesion(_ => RespuestaLogin());

        var restaurada = await sesion.RestaurarAsync();

        Assert.False(restaurada);
        Assert.False(sesion.IsAuthenticated);
    }

    private static (SesionService Sesion, TokenStore Token, EntornoApi Entorno) CrearSesion(
        Func<SolicitudRegistrada, HttpResponseMessage> responder)
    {
        var entorno = FabricaApi.Crear(responder);
        var almacen = new AlmacenSesiones();
        var storage = AlmacenamientoFalso.Crear();
        var persistencia = new SesionPersistencia(almacen, storage);
        var sesion = new SesionService(entorno.Api, entorno.Token, persistencia);

        return (sesion, entorno.Token, entorno);
    }

    private static HttpResponseMessage RespuestaLogin(
        Guid? empresaId = null,
        string rol = "AdministradorGeneral",
        string etiqueta = "Toda la plataforma") =>
        Respuestas.Json(HttpStatusCode.OK, new
        {
            token = "jwt-de-prueba",
            expira = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            usuario = new
            {
                id = Guid.NewGuid(),
                nombre = "Usuario de prueba",
                email = "usuario@siga.demo",
                rol,
                empresaId,
                empresaNombre = empresaId is null ? null : "Agro del Sur",
                etiquetaAlcance = etiqueta,
                establecimientoIds = Array.Empty<Guid>()
            }
        });
}
