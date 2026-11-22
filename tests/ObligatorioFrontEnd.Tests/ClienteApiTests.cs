using System.Net;
using System.Text.Json;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Tests.Infraestructura;

namespace ObligatorioFrontEnd.Tests;

public sealed class ClienteApiTests
{
    private const string RutaEmpresas = "/api/v1/empresas";

    private const string EmpresaJson = """
        {
          "id": "11111111-1111-1111-1111-111111111111",
          "nombre": "Agro del Sur",
          "razonSocial": "Agro del Sur S.A.",
          "activo": true,
          "fechaCreacion": "2026-01-01T00:00:00Z",
          "version": 1
        }
        """;

    private const string CampanaJson = """
        {
          "id": "22222222-2222-2222-2222-222222222222",
          "empresaId": "11111111-1111-1111-1111-111111111111",
          "nombre": "Soja 2026",
          "cultivoId": "33333333-3333-3333-3333-333333333333",
          "cultivoNombre": "Soja",
          "fechaInicio": "2026-03-01T00:00:00Z",
          "fechaFin": null,
          "estado": "Activa",
          "fechaCreacion": "2026-01-15T00:00:00Z",
          "version": 4,
          "potreros": null
        }
        """;

    private const string FotografiaJson = """
        {
          "id": "44444444-4444-4444-4444-444444444444",
          "eventoId": "55555555-5555-5555-5555-555555555555",
          "fechaCaptura": "2026-03-10T12:00:00Z",
          "latitud": -34.9,
          "longitud": -56.1,
          "archivo": "fotos/abc.png",
          "visible": true,
          "motivoOcultamiento": null,
          "fechaCreacion": "2026-03-10T12:00:00Z",
          "version": 1
        }
        """;

    [Fact]
    public void BaseAddress_ExponeLaUrlDelCliente()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.Json(HttpStatusCode.OK, "[]"));

        Assert.Equal(new Uri(FabricaApi.BaseUrl), entorno.Api.BaseAddress);
    }

    [Fact]
    public async Task GetAsync_AgregaElBearerToken()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.Json(HttpStatusCode.OK, "[]"));
        entorno.Token.Guardar("jwt-de-prueba");

        await entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas);

        var solicitud = Assert.Single(entorno.Manejador.Solicitudes);
        Assert.Equal(HttpMethod.Get, solicitud.Metodo);
        Assert.Equal("Bearer jwt-de-prueba", solicitud.Autorizacion);
    }

    [Fact]
    public async Task GetAsync_DeserializaCamelCaseYEnumsComoTexto()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.Json(HttpStatusCode.OK, CampanaJson));

        var campana = await entorno.Api.GetAsync<CampanaDto>("/api/v1/campanas/1");

        Assert.Equal("Soja 2026", campana.Nombre);
        Assert.Equal(EstadoCampana.Activa, campana.Estado);
        Assert.Equal(4, campana.Version);
        Assert.Empty(campana.Potreros);
    }

    [Fact]
    public async Task GetAsync_ReutilizaLaCacheParaLaMismaRuta()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.Json(HttpStatusCode.OK, $"[{EmpresaJson}]"));

        var primera = await entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas);
        var segunda = await entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas);

        Assert.Single(entorno.Manejador.Solicitudes);
        Assert.Equal(primera[0].Id, segunda[0].Id);
    }

    [Fact]
    public async Task GetAsync_NoComparteCacheEntreRutasDistintas()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.Json(HttpStatusCode.OK, "[]"));

        await entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas);
        await entorno.Api.GetAsync<List<EmpresaDto>>($"{RutaEmpresas}?incluirInactivas=true");

        Assert.Equal(2, entorno.Manejador.Cantidad);
    }

    [Fact]
    public async Task PostAsync_LimpiaLaCache()
    {
        var entorno = FabricaApi.Crear(solicitud => solicitud.Metodo == HttpMethod.Post
            ? Respuestas.Json(HttpStatusCode.Created, EmpresaJson)
            : Respuestas.Json(HttpStatusCode.OK, $"[{EmpresaJson}]"));

        await entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas);
        await entorno.Api.PostAsync<EmpresaDto>(RutaEmpresas, new CrearEmpresaDto { Nombre = "Nueva" });
        await entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas);

        Assert.Equal(3, entorno.Manejador.Cantidad);
        Assert.Equal(
            new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Get },
            entorno.Manejador.Solicitudes.Select(solicitud => solicitud.Metodo));
    }

    [Fact]
    public async Task PostAsync_SerializaCamelCaseYEnumsComoTexto()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.Json(HttpStatusCode.OK, CampanaJson));

        await entorno.Api.PutAsync<CampanaDto>("/api/v1/campanas/1", new ActualizarCampanaDto
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Nombre = "Soja 2026",
            Estado = EstadoCampana.Activa,
            FechaInicio = new DateTime(2026, 3, 1),
            Version = 4
        });

        var cuerpo = Assert.Single(entorno.Manejador.Solicitudes).Cuerpo;
        Assert.NotNull(cuerpo);
        Assert.Contains("\"nombre\":\"Soja 2026\"", cuerpo);
        Assert.Contains("\"estado\":\"Activa\"", cuerpo);
        Assert.DoesNotContain("\"Nombre\"", cuerpo);
    }

    [Fact]
    public async Task GetAsync_ReintentaAnteUnErrorTransitorio()
    {
        var entorno = FabricaApi.Crear((_, intento) => intento == 1
            ? Respuestas.SinContenido(HttpStatusCode.BadGateway)
            : Respuestas.Json(HttpStatusCode.OK, "[]"));

        var resultado = await entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas);

        Assert.Empty(resultado);
        Assert.Equal(2, entorno.Manejador.Cantidad);
    }

    [Fact]
    public async Task GetAsync_DejaDeReintentarTrasTresIntentos()
    {
        var entorno = FabricaApi.Crear(_ =>
            Respuestas.SinContenido(HttpStatusCode.ServiceUnavailable));

        var error = await Assert.ThrowsAsync<ApiException>(
            () => entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas));

        Assert.Equal(503, error.Status);
        Assert.Equal(3, entorno.Manejador.Cantidad);
    }

    [Fact]
    public async Task PostAsync_NoReintentaAnteUnErrorTransitorio()
    {
        var entorno = FabricaApi.Crear(_ =>
            Respuestas.SinContenido(HttpStatusCode.ServiceUnavailable));

        var error = await Assert.ThrowsAsync<ApiException>(
            () => entorno.Api.PostAsync<EmpresaDto>(RutaEmpresas, new CrearEmpresaDto()));

        Assert.Equal(503, error.Status);
        Assert.Single(entorno.Manejador.Solicitudes);
    }

    [Fact]
    public async Task GetAsync_SinConexion_DevuelveStatusCero()
    {
        var entorno = FabricaApi.Crear(new ManejadorFalso(_ =>
            throw new HttpRequestException("La API no está disponible.")));

        var error = await Assert.ThrowsAsync<ApiException>(
            () => entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas));

        Assert.Equal(0, error.Status);
        Assert.Contains("No se pudo conectar", error.Message);
        Assert.Equal(3, entorno.Manejador.Cantidad);
    }

    [Fact]
    public async Task Validar_401ConToken_ExpiraLaSesion()
    {
        var entorno = FabricaApi.Crear(_ =>
            Respuestas.Problema(HttpStatusCode.Unauthorized, "No autorizado", "Token vencido."));
        entorno.Token.Guardar("jwt-vencido");

        var expirada = false;
        entorno.Token.Expirada += () => expirada = true;

        var error = await Assert.ThrowsAsync<ApiException>(
            () => entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas));

        Assert.Equal(401, error.Status);
        Assert.Null(entorno.Token.Token);
        Assert.True(expirada);
        Assert.Contains("sesión expiró", error.Message);
    }

    [Fact]
    public async Task Validar_401SinToken_NoExpiraLaSesion()
    {
        var entorno = FabricaApi.Crear(_ =>
            Respuestas.Problema(HttpStatusCode.Unauthorized, "No autorizado", "Credenciales inválidas."));

        var expirada = false;
        entorno.Token.Expirada += () => expirada = true;

        var error = await Assert.ThrowsAsync<ApiException>(
            () => entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas));

        Assert.Equal(401, error.Status);
        Assert.Equal("Credenciales inválidas.", error.Message);
        Assert.False(expirada);
    }

    [Fact]
    public async Task Validar_403_UsaElMensajeDePermisos()
    {
        var entorno = FabricaApi.Crear(_ =>
            Respuestas.Problema(HttpStatusCode.Forbidden, "Prohibido", "Detalle interno de la API."));

        var error = await Assert.ThrowsAsync<ApiException>(
            () => entorno.Api.GetAsync<List<EmpresaDto>>(RutaEmpresas));

        Assert.Equal(403, error.Status);
        Assert.Equal("No tenés permisos para esta operación.", error.Message);
    }

    [Fact]
    public async Task Validar_400_IncluyeLosErroresDeValidacion()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.Problema(
            HttpStatusCode.BadRequest,
            "Error de validación",
            "Hay campos inválidos.",
            new Dictionary<string, string[]>
            {
                ["Nombre"] = ["El nombre es obligatorio."],
                ["Email"] = ["El email no es válido."]
            }));

        var error = await Assert.ThrowsAsync<ApiException>(
            () => entorno.Api.PostAsync<EmpresaDto>(RutaEmpresas, new CrearEmpresaDto()));

        Assert.Equal(400, error.Status);
        Assert.Equal("Hay campos inválidos.", error.Message);
        Assert.Equal(2, error.Errores.Count);
        Assert.Contains("El nombre es obligatorio.", error.Errores);
    }

    [Fact]
    public async Task GetBytesAsync_404_DevuelveNull()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.SinContenido(HttpStatusCode.NotFound));

        var contenido = await entorno.Api.GetBytesAsync("/api/v1/eventos/1/fotografias/2/archivo");

        Assert.Null(contenido);
    }

    [Fact]
    public async Task GetBytesAsync_200_DevuelveElBinario()
    {
        byte[] esperado = [1, 2, 3, 4];
        var entorno = FabricaApi.Crear(_ => Respuestas.Binario(HttpStatusCode.OK, esperado));

        var contenido = await entorno.Api.GetBytesAsync("/api/v1/eventos/1/fotografias/2/archivo");

        Assert.NotNull(contenido);
        Assert.Equal(esperado, contenido);
    }

    [Fact]
    public async Task PostMultipartAsync_DevuelveLaRespuestaYLimpiaLaCache()
    {
        var entorno = FabricaApi.Crear(solicitud => solicitud.Metodo == HttpMethod.Get
            ? Respuestas.Json(HttpStatusCode.OK, "[]")
            : Respuestas.Json(HttpStatusCode.Created, FotografiaJson));

        await entorno.Api.GetAsync<List<FotografiaDto>>("/api/v1/eventos/1/fotografias");

        using var formulario = new MultipartFormDataContent();
        formulario.Add(new ByteArrayContent([1, 2, 3]), "archivo", "foto.png");

        var foto = await entorno.Api.PostMultipartAsync<FotografiaDto>(
            "/api/v1/eventos/1/fotografias",
            formulario);

        await entorno.Api.GetAsync<List<FotografiaDto>>("/api/v1/eventos/1/fotografias");

        Assert.Equal("abc.png", foto.NombreArchivo);
        Assert.Equal(2, entorno.Manejador.Solicitudes.Count(s => s.Metodo == HttpMethod.Get));
    }

    [Fact]
    public async Task PostSinRespuestaAsync_204_NoFalla()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.SinContenido(HttpStatusCode.NoContent));

        await entorno.Api.PostSinRespuestaAsync("/api/v1/campanas/1/potreros/2/cerrar");

        var solicitud = Assert.Single(entorno.Manejador.Solicitudes);
        Assert.Equal(HttpMethod.Post, solicitud.Metodo);
        Assert.Equal("/api/v1/campanas/1/potreros/2/cerrar", solicitud.Ruta);
    }

    [Fact]
    public async Task DeleteAsync_204_NoFalla()
    {
        var entorno = FabricaApi.Crear(_ => Respuestas.SinContenido(HttpStatusCode.NoContent));

        await entorno.Api.DeleteAsync("/api/v1/empresas/1");

        var solicitud = Assert.Single(entorno.Manejador.Solicitudes);
        Assert.Equal(HttpMethod.Delete, solicitud.Metodo);
    }

    [Fact]
    public void Json_EstaConfiguradoParaWebYEnums()
    {
        var campana = JsonSerializer.Deserialize<CampanaDto>(CampanaJson, ClienteApi.Json);

        Assert.NotNull(campana);
        Assert.Equal(EstadoCampana.Activa, campana.Estado);
    }
}
