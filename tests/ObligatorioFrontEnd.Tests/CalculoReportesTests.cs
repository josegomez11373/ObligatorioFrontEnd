using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;
using ObligatorioFrontEnd.Services;

namespace ObligatorioFrontEnd.Tests;

/// <summary>
/// Pruebas de las agregaciones del tablero. Los datos son fijos para que los
/// totales, los filtros y el aislamiento por empresa sean verificables.
/// </summary>
public sealed class CalculoReportesTests
{
    [Fact]
    public void Build_SinFiltros_CalculaLosTotales()
    {
        var tablero = Fixture.Build();

        Assert.Equal(2, tablero.Campanas);
        Assert.Equal(1, tablero.CampanasActivas);
        Assert.Equal(1, tablero.CampanasCerradas);
        Assert.Equal(3, tablero.Eventos);
        Assert.Equal(2, tablero.EventosRegistrados);
        Assert.Equal(1, tablero.EventosAnulados);
        Assert.Equal(3, tablero.Tareas);
        Assert.Equal(1, tablero.TareasPendientes);
        Assert.Equal(1, tablero.TareasRealizadas);
        Assert.Equal(0, tablero.TareasVerificadas);
        Assert.Equal(150m, tablero.CostoTareas);
        Assert.Equal(2, tablero.RecursosUtilizados);
        Assert.Equal(3, tablero.EventosPorTipo.Count);
        Assert.Equal(3, tablero.EventosConAutor.Count);
        Assert.Equal(6, tablero.LineaDeTiempo.Count);
    }

    [Fact]
    public void Build_DetalleDeCampanas_OrdenaPorFechaYCuentaAsignaciones()
    {
        var filas = Fixture.Build().CampanasDetalle;

        Assert.Equal(2, filas.Count);

        var soja = filas[0];
        Assert.Equal("Soja 2026", soja.Nombre);
        Assert.Equal("Agro del Sur", soja.Empresa);
        Assert.Equal(EstadoCampana.Activa, soja.Estado);
        Assert.Equal(1, soja.Potreros);
        Assert.Equal(2, soja.Eventos);
        Assert.Equal(1, soja.Tareas);
        Assert.Equal(100m, soja.CostoTareas);

        var maiz = filas[1];
        Assert.Equal("Maíz 2026", maiz.Nombre);
        Assert.Equal(1, maiz.Potreros);
        Assert.Equal(1, maiz.Eventos);
        Assert.Equal(1, maiz.Tareas);
        Assert.Equal(50m, maiz.CostoTareas);
    }

    [Fact]
    public void Build_FiltraPorEmpresa_YNoMezclaDatosDeOtrasEmpresas()
    {
        var tablero = Fixture.Build(new ReportFilter { EmpresaId = Fixture.EmpresaA });

        Assert.Equal(1, tablero.Campanas);
        Assert.Equal(1, tablero.CampanasActivas);
        Assert.Equal(0, tablero.CampanasCerradas);
        Assert.Equal(2, tablero.Eventos);
        Assert.Equal(1, tablero.EventosRegistrados);
        Assert.Equal(1, tablero.EventosAnulados);
        Assert.Equal(2, tablero.Tareas);
        Assert.Equal(1, tablero.TareasRealizadas);
        Assert.Equal(100m, tablero.CostoTareas);

        Assert.Single(tablero.CampanasDetalle);
        Assert.Equal("Soja 2026", tablero.CampanasDetalle[0].Nombre);
        Assert.All(tablero.CampanasDetalle, fila => Assert.Equal("Agro del Sur", fila.Empresa));
        Assert.Single(tablero.CostosPorCampana);
        Assert.Single(tablero.ProblemasDetectados);
        Assert.Single(tablero.ZonasProblematicas);
        Assert.Single(tablero.ActividadReciente);
        Assert.Equal("Admin A", tablero.ActividadReciente[0].Actor);
        Assert.All(tablero.EventosConAutor, fila => Assert.Equal("Potrero 1", fila.Potrero));
    }

    [Fact]
    public void Build_FiltraPorEstablecimiento()
    {
        var tablero = Fixture.Build(new ReportFilter { EstablecimientoId = Fixture.EstablecimientoA });

        Assert.Single(tablero.CampanasDetalle);
        Assert.Equal("Soja 2026", tablero.CampanasDetalle[0].Nombre);
        Assert.Equal(2, tablero.Eventos);
        Assert.Equal(2, tablero.Tareas);
        Assert.All(tablero.EventosConAutor, fila => Assert.Equal("Potrero 1", fila.Potrero));
        Assert.Single(tablero.ProblemasDetectados);
        Assert.Single(tablero.ZonasProblematicas);
    }

    [Fact]
    public void Build_FiltraPorPotrero()
    {
        var tablero = Fixture.Build(new ReportFilter { PotreroId = Fixture.PotreroB });

        Assert.Single(tablero.CampanasDetalle);
        Assert.Equal("Maíz 2026", tablero.CampanasDetalle[0].Nombre);
        Assert.Equal(1, tablero.Eventos);
        Assert.Equal(1, tablero.Tareas);
        Assert.Equal(50m, tablero.CostoTareas);
        Assert.Equal("Enfermedad", tablero.ProblemasDetectados[0].Tipo);
        Assert.Equal("Potrero 2", tablero.ProblemasDetectados[0].Potrero);
    }

    [Fact]
    public void Build_FiltraPorEstadoDeCampana()
    {
        var tablero = Fixture.Build(new ReportFilter { CampanaEstado = EstadoCampana.Activa });

        Assert.Equal(1, tablero.Campanas);
        Assert.Equal(1, tablero.CampanasActivas);
        Assert.Equal(0, tablero.CampanasCerradas);
        Assert.Equal(2, tablero.Eventos);
        Assert.Equal(2, tablero.Tareas);
    }

    [Fact]
    public void Build_FiltraPorEstadoDeEvento()
    {
        var tablero = Fixture.Build(new ReportFilter { EventoEstado = EstadoEvento.Anulado });

        Assert.Equal(1, tablero.Eventos);
        Assert.Equal(0, tablero.EventosRegistrados);
        Assert.Equal(1, tablero.EventosAnulados);
        Assert.Empty(tablero.ProblemasDetectados);
        Assert.Single(tablero.EventosConAutor);
        Assert.Equal(4, tablero.LineaDeTiempo.Count);
    }

    [Fact]
    public void Build_FiltraPorEstadoDeTarea()
    {
        var tablero = Fixture.Build(new ReportFilter { TareaEstado = EstadoTarea.Anulada });

        Assert.Equal(1, tablero.Tareas);
        Assert.Equal(0, tablero.TareasPendientes);
        Assert.Equal(0, tablero.TareasRealizadas);
        Assert.Equal(0m, tablero.CostoTareas);
        Assert.Equal(0, tablero.RecursosUtilizados);
        Assert.Empty(tablero.Tratamientos);
        Assert.Empty(tablero.RecursosDetalle);
        Assert.Empty(tablero.TareasDeRespuesta);
        Assert.Equal(4, tablero.LineaDeTiempo.Count);
    }

    [Fact]
    public void Build_FiltraPorRangoDeFechas()
    {
        var tablero = Fixture.Build(new ReportFilter
        {
            Desde = new DateTime(2026, 3, 1),
            Hasta = new DateTime(2026, 3, 11)
        });

        Assert.Equal(1, tablero.Campanas);
        Assert.Equal(1, tablero.Eventos);
        Assert.Equal(1, tablero.Tareas);
        Assert.Equal(2, tablero.LineaDeTiempo.Count);
        Assert.Single(tablero.ProblemasDetectados);
    }

    [Fact]
    public void Build_ExcluyeTareasAnuladasDeCostosYRecursos()
    {
        var tablero = Fixture.Build();

        Assert.Equal(150m, tablero.CostoTareas);
        Assert.Equal(2, tablero.RecursosUtilizados);

        var soja = tablero.CostosPorCampana.Single(fila => fila.Campana == "Soja 2026");
        Assert.Equal(1, soja.Tareas);
        Assert.Equal(1, soja.TareasAnuladas);
        Assert.Equal(40m, soja.Insumos);
        Assert.Equal(60m, soja.Maquinaria);
        Assert.Equal(100m, soja.Total);

        Assert.DoesNotContain(tablero.RecursosDetalle, fila => fila.Nombre == "Agua");
    }

    [Fact]
    public void Build_DetectaProblemasPorNombreDeTipo()
    {
        var tablero = Fixture.Build();

        Assert.Equal(2, tablero.ProblemasDetectados.Count);
        Assert.Contains(tablero.ProblemasDetectados, fila => fila.Tipo == "Plaga" && fila.Cantidad == 1);
        Assert.Contains(tablero.ProblemasDetectados, fila => fila.Tipo == "Enfermedad" && fila.Cantidad == 1);
        Assert.DoesNotContain(tablero.ProblemasDetectados, fila => fila.Tipo == "Maleza");
    }

    [Fact]
    public void Build_AgrupaRecursosPorTipoYNombre()
    {
        var filas = Fixture.Build().RecursosDetalle;

        Assert.Equal(2, filas.Count);
        Assert.Equal("Maquinaria", filas[0].Tipo);
        Assert.Equal("Pulverizadora", filas[0].Nombre);
        Assert.Equal(1m, filas[0].Cantidad);
        Assert.Equal(60m, filas[0].Costo);

        Assert.Equal("Insumo", filas[1].Tipo);
        Assert.Equal("Glifosato", filas[1].Nombre);
        Assert.Equal(2m, filas[1].Cantidad);
        Assert.Equal(40m, filas[1].Costo);
    }

    [Fact]
    public void Build_CalculaEfectividadDeTratamientos()
    {
        var tratamiento = Assert.Single(Fixture.Build().Tratamientos);

        Assert.Equal("Fumigación", tratamiento.Tratamiento);
        Assert.Equal(2, tratamiento.Total);
        Assert.Equal(1, tratamiento.Resueltas);
        Assert.Equal(50d, tratamiento.Efectividad);
        Assert.Equal(75m, tratamiento.CostoPromedio);
    }

    [Fact]
    public void Build_ActividadReciente_OrdenaYLimitaAVeinte()
    {
        var auditoria = Enumerable.Range(0, 25)
            .Select(indice => new AuditoriaDto(
                Guid.NewGuid(),
                Fixture.EmpresaA,
                null,
                $"Actor {indice}",
                null,
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(indice),
                "Campana",
                Guid.NewGuid(),
                AccionAuditoria.Alta,
                null,
                null))
            .ToList();

        var tablero = Fixture.Build(auditoria: auditoria);

        Assert.Equal(20, tablero.ActividadReciente.Count);
        Assert.Equal("Actor 24", tablero.ActividadReciente[0].Actor);
        Assert.Equal("Actor 5", tablero.ActividadReciente[^1].Actor);
    }

    [Fact]
    public void Build_SinDatos_DevuelveUnTableroVacio()
    {
        var tablero = CalculoReportes.Build(
            new ReportFilter(),
            [],
            [],
            [],
            [],
            [],
            [],
            []);

        Assert.Equal(0, tablero.Campanas);
        Assert.Equal(0, tablero.Eventos);
        Assert.Equal(0, tablero.Tareas);
        Assert.Equal(0m, tablero.CostoTareas);
        Assert.Empty(tablero.CampanasDetalle);
        Assert.Empty(tablero.LineaDeTiempo);
        Assert.Empty(tablero.ProblemasDetectados);
        Assert.Empty(tablero.CostosPorCampana);
        Assert.Empty(tablero.ZonasProblematicas);
    }

    /// <summary>Datos compartidos por todas las pruebas del tablero.</summary>
    private static class Fixture
    {
        public static readonly Guid EmpresaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid EmpresaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid EstablecimientoA = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
        public static readonly Guid EstablecimientoB = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
        public static readonly Guid PotreroA = Guid.Parse("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2");
        public static readonly Guid PotreroB = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
        public static readonly Guid AsignacionA = Guid.Parse("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3");
        public static readonly Guid AsignacionB = Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3");
        public static readonly Guid CampanaA = Guid.Parse("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a4");
        public static readonly Guid CampanaB = Guid.Parse("b4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b4");
        public static readonly Guid EventoA1 = Guid.Parse("a5a5a5a5-a5a5-a5a5-a5a5-a5a5a5a5a5a5");
        public static readonly Guid EventoA2 = Guid.Parse("a6a6a6a6-a6a6-a6a6-a6a6-a6a6a6a6a6a6");
        public static readonly Guid EventoB1 = Guid.Parse("b5b5b5b5-b5b5-b5b5-b5b5-b5b5b5b5b5b5");

        public static ReportDashboard Build(
            ReportFilter? filtro = null,
            IReadOnlyList<AuditoriaDto>? auditoria = null) =>
            CalculoReportes.Build(
                filtro ?? new ReportFilter(),
                Campanas(),
                Potreros(),
                Empresas(),
                Establecimientos(),
                Eventos(),
                Tareas(),
                auditoria ?? Auditoria());

        private static List<EmpresaDto> Empresas() =>
        [
            new(EmpresaA, "Agro del Sur", "Agro del Sur S.A.", true, new DateTime(2026, 1, 1), 1),
            new(EmpresaB, "Campos Verdes", null, true, new DateTime(2026, 1, 1), 1)
        ];

        private static List<EstablecimientoDto> Establecimientos() =>
        [
            new(EstablecimientoA, EmpresaA, "Agro del Sur", "La Esperanza", null, null, 420, true, new DateTime(2026, 1, 1), 1),
            new(EstablecimientoB, EmpresaB, "Campos Verdes", "El Ombú", null, null, 300, true, new DateTime(2026, 1, 1), 1)
        ];

        private static List<PotreroDto> Potreros() =>
        [
            new(PotreroA, EmpresaA, EstablecimientoA, "La Esperanza", "Potrero 1", 120, null, true, new DateTime(2026, 1, 1), 1),
            new(PotreroB, EmpresaB, EstablecimientoB, "El Ombú", "Potrero 2", 80, null, true, new DateTime(2026, 1, 1), 1)
        ];

        private static List<CampanaDto> Campanas() =>
        [
            new(
                CampanaA,
                EmpresaA,
                "Soja 2026",
                Guid.NewGuid(),
                "Soja",
                new DateTime(2026, 3, 1),
                new DateTime(2026, 6, 30),
                EstadoCampana.Activa,
                new DateTime(2026, 1, 15),
                3)
            {
                Potreros =
                [
                    new(AsignacionA, PotreroA, "Potrero 1", new DateTime(2026, 3, 1), new DateTime(2026, 6, 30), EstadoCampana.Activa, 1)
                ]
            },
            new(
                CampanaB,
                EmpresaB,
                "Maíz 2026",
                Guid.NewGuid(),
                "Maíz",
                new DateTime(2026, 2, 1),
                new DateTime(2026, 5, 31),
                EstadoCampana.Cerrada,
                new DateTime(2026, 1, 10),
                2)
            {
                Potreros =
                [
                    new(AsignacionB, PotreroB, "Potrero 2", new DateTime(2026, 2, 1), new DateTime(2026, 5, 31), EstadoCampana.Cerrada, 1)
                ]
            }
        ];

        private static List<EventoDto> Eventos() =>
        [
            Evento(EventoA1, EmpresaA, AsignacionA, "Plaga", new DateTime(2026, 3, 10), EstadoEvento.Registrado, "Pulgón", "Ing. Laura"),
            Evento(EventoA2, EmpresaA, AsignacionA, "Maleza", new DateTime(2026, 3, 12), EstadoEvento.Anulado, "Yuyo colorado", "Operario"),
            Evento(EventoB1, EmpresaB, AsignacionB, "Enfermedad", new DateTime(2026, 3, 15), EstadoEvento.Registrado, "Roya", "Ing. Pablo")
        ];

        private static EventoDto Evento(
            Guid id,
            Guid empresaId,
            Guid asignacionId,
            string tipo,
            DateTime fecha,
            EstadoEvento estado,
            string descripcion,
            string autor) =>
            new(
                id,
                empresaId,
                asignacionId,
                tipo == "Plaga" ? "Potrero 1" : tipo == "Maleza" ? "Potrero 1" : "Potrero 2",
                Guid.NewGuid(),
                tipo,
                fecha,
                descripcion,
                null,
                null,
                null,
                Guid.NewGuid(),
                autor,
                estado,
                null,
                fecha,
                1);

        private static List<TareaDto> Tareas() =>
        [
            Tarea(
                EmpresaA,
                AsignacionA,
                EventoA1,
                "Fumigación",
                new DateTime(2026, 3, 11),
                EstadoTarea.Realizada,
                100m,
                true,
                [
                    new(TipoRecursoTarea.Insumo, Guid.NewGuid(), "Glifosato", 2, 20m, 40m),
                    new(TipoRecursoTarea.Maquinaria, Guid.NewGuid(), "Pulverizadora", 1, 60m, 60m)
                ]),
            Tarea(
                EmpresaA,
                AsignacionA,
                null,
                "Riego",
                new DateTime(2026, 3, 13),
                EstadoTarea.Anulada,
                999m,
                null,
                [new(TipoRecursoTarea.Insumo, Guid.NewGuid(), "Agua", 10, 50m, 500m)]),
            Tarea(
                EmpresaB,
                AsignacionB,
                null,
                "Fumigación",
                new DateTime(2026, 3, 16),
                EstadoTarea.Planificada,
                50m,
                null,
                [])
        ];

        private static TareaDto Tarea(
            Guid empresaId,
            Guid asignacionId,
            Guid? eventoId,
            string tipo,
            DateTime fechaPlanificada,
            EstadoTarea estado,
            decimal costo,
            bool? soluciono,
            IReadOnlyList<TareaRecursoDto> recursos) =>
            new(
                Guid.NewGuid(),
                empresaId,
                asignacionId,
                "Potrero 1",
                eventoId,
                Guid.NewGuid(),
                tipo,
                "Descripción de la tarea",
                fechaPlanificada,
                estado == EstadoTarea.Realizada ? fechaPlanificada : null,
                estado == EstadoTarea.Realizada ? "Aplicado" : null,
                null,
                soluciono,
                null,
                null,
                null,
                "Ing. Laura",
                null,
                estado,
                null,
                costo,
                fechaPlanificada,
                1)
            {
                Recursos = recursos
            };

        private static List<AuditoriaDto> Auditoria() =>
        [
            new(
                Guid.NewGuid(),
                EmpresaA,
                null,
                "Admin A",
                null,
                new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                "Campana",
                CampanaA,
                AccionAuditoria.Alta,
                null,
                null),
            new(
                Guid.NewGuid(),
                EmpresaB,
                null,
                "Admin B",
                null,
                new DateTime(2026, 3, 11, 12, 0, 0, DateTimeKind.Utc),
                "Evento",
                EventoB1,
                AccionAuditoria.Edicion,
                null,
                null)
        ];
    }
}
