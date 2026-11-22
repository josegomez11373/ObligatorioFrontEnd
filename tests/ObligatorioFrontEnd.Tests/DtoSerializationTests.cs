using System.Text.Json;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Tests;

/// <summary>
/// Verifica que los DTOs espejo toleren las variantes reales de la API:
/// camelCase, enums como texto y colecciones omitidas o nulas.
/// </summary>
public sealed class DtoSerializationTests
{
    private static readonly JsonSerializerOptions Json = ClienteApi.Json;

    [Fact]
    public void CampanaDto_NormalizaPotrerosNulos()
    {
        const string json = """
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

        var campana = JsonSerializer.Deserialize<CampanaDto>(json, Json);

        Assert.NotNull(campana);
        Assert.Equal(EstadoCampana.Activa, campana.Estado);
        Assert.Empty(campana.Potreros);
    }

    [Fact]
    public void EventoDto_NormalizaFotografiasNulas()
    {
        const string json = """
            {
              "id": "55555555-5555-5555-5555-555555555555",
              "empresaId": "11111111-1111-1111-1111-111111111111",
              "campanaPotreroId": "66666666-6666-6666-6666-666666666666",
              "potreroNombre": "Potrero 1",
              "tipoEventoId": "77777777-7777-7777-7777-777777777777",
              "tipoEventoNombre": "Plaga",
              "fecha": "2026-03-10T00:00:00Z",
              "descripcion": "Pulgón",
              "ubicacion": null,
              "latitud": -34.9,
              "longitud": -56.1,
              "registradoPorId": null,
              "registradoPor": "Ing. Laura",
              "estado": "Registrado",
              "motivoAnulacion": null,
              "fechaCreacion": "2026-03-10T00:00:00Z",
              "version": 1,
              "fotografias": null
            }
            """;

        var evento = JsonSerializer.Deserialize<EventoDto>(json, Json);

        Assert.NotNull(evento);
        Assert.Equal(EstadoEvento.Registrado, evento.Estado);
        Assert.Equal(-34.9, evento.Latitud);
        Assert.Empty(evento.Fotografias);
    }

    [Fact]
    public void TareaDto_NormalizaRecursosNulos()
    {
        const string json = """
            {
              "id": "88888888-8888-8888-8888-888888888888",
              "empresaId": "11111111-1111-1111-1111-111111111111",
              "campanaPotreroId": "66666666-6666-6666-6666-666666666666",
              "potreroNombre": "Potrero 1",
              "eventoId": null,
              "tipoTareaId": "99999999-9999-9999-9999-999999999999",
              "tipoTareaNombre": "Fumigación",
              "descripcion": "Aplicar herbicida",
              "fechaPlanificada": "2026-03-11T00:00:00Z",
              "fechaRealizacion": null,
              "resultado": null,
              "fechaComprobacion": null,
              "solucionoProblema": null,
              "ejecutorId": null,
              "ejecutorNombre": null,
              "ejecutorTipo": null,
              "asignadaPor": "Ing. Laura",
              "verificadaPor": null,
              "estado": "Planificada",
              "motivoAnulacion": null,
              "costo": 100.50,
              "fechaCreacion": "2026-03-01T00:00:00Z",
              "version": 2,
              "recursos": null
            }
            """;

        var tarea = JsonSerializer.Deserialize<TareaDto>(json, Json);

        Assert.NotNull(tarea);
        Assert.Equal(EstadoTarea.Planificada, tarea.Estado);
        Assert.Equal(100.50m, tarea.Costo);
        Assert.Empty(tarea.Recursos);
    }

    [Fact]
    public void UsuarioSesionDto_NormalizaEstablecimientosNulos()
    {
        const string json = """
            {
              "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              "nombre": "Administrador general",
              "email": "admin@siga.demo",
              "rol": "AdministradorGeneral",
              "empresaId": null,
              "empresaNombre": null,
              "etiquetaAlcance": "Toda la plataforma",
              "establecimientoIds": null
            }
            """;

        var usuario = JsonSerializer.Deserialize<UsuarioSesionDto>(json, Json);

        Assert.NotNull(usuario);
        Assert.Equal(RolApi.AdministradorGeneral, usuario.Rol);
        Assert.Empty(usuario.EstablecimientoIds);
    }

    [Fact]
    public void AuditoriaPaginadaDto_NormalizaItemsNulos()
    {
        const string json = """
            {
              "items": null,
              "pagina": 1,
              "tamanoPagina": 200,
              "total": 0
            }
            """;

        var pagina = JsonSerializer.Deserialize<AuditoriaPaginadaDto>(json, Json);

        Assert.NotNull(pagina);
        Assert.Empty(pagina.Items);
        Assert.Equal(200, pagina.TamanoPagina);
    }

    [Fact]
    public void EnumDesconocido_FallaEnLaDeserializacion()
    {
        const string json = """
            {
              "id": "22222222-2222-2222-2222-222222222222",
              "empresaId": "11111111-1111-1111-1111-111111111111",
              "nombre": "Soja 2026",
              "cultivoId": "33333333-3333-3333-3333-333333333333",
              "cultivoNombre": "Soja",
              "fechaInicio": "2026-03-01T00:00:00Z",
              "estado": "EstadoQueNoExiste",
              "fechaCreacion": "2026-01-15T00:00:00Z",
              "version": 4
            }
            """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CampanaDto>(json, Json));
    }

    [Fact]
    public void FotografiaDto_DevuelveSoloElNombreDelArchivo()
    {
        var foto = new FotografiaDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            null,
            null,
            "fotos/2026/abc-123.png",
            true,
            null,
            DateTime.UtcNow,
            1);

        Assert.Equal("abc-123.png", foto.NombreArchivo);
    }

    [Fact]
    public void DemoUser_CalculaLasIniciales()
    {
        var usuario = new DemoUser(
            Guid.NewGuid(),
            "laura.mendez@siga.demo",
            "Laura Méndez",
            UserRole.IngenieroAgronomo,
            Guid.NewGuid(),
            [],
            "Agro del Sur");

        Assert.Equal("LM", usuario.Initials);
    }

    [Fact]
    public void ApiException_SeComportaComoInvalidOperationException()
    {
        var error = new ApiException(409, "Conflicto de versión.");

        Assert.IsAssignableFrom<InvalidOperationException>(error);
        Assert.Equal(409, error.Status);
        Assert.Empty(error.Errores);
    }
}
