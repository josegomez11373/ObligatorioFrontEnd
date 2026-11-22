using ObligatorioFrontEnd.Models;
using ObligatorioFrontEnd.Services;

namespace ObligatorioFrontEnd.Tests;

/// <summary>
/// Fija la matriz de permisos de la sección 4.1 del documento v2. Cualquier
/// cambio accidental en <see cref="Permissions"/> rompe estas pruebas.
/// </summary>
public sealed class PermissionsTests
{
    private static readonly UserRole[] Roles = Enum.GetValues<UserRole>();
    private static readonly SigaModule[] Modulos = Enum.GetValues<SigaModule>();

    public static IEnumerable<object[]> MatrizDeVista()
    {
        foreach (var rol in Roles)
        {
            foreach (var modulo in Modulos)
            {
                yield return [rol, modulo, ModulosVisibles(rol).Contains(modulo)];
            }
        }
    }

    public static IEnumerable<object[]> MatrizDeGestion()
    {
        foreach (var rol in Roles)
        {
            foreach (var modulo in Modulos)
            {
                yield return [rol, modulo, ModulosGestionables(rol).Contains(modulo)];
            }
        }
    }

    [Theory]
    [MemberData(nameof(MatrizDeVista))]
    public void CanView_RespetaLaMatriz(UserRole rol, SigaModule modulo, bool esperado)
    {
        Assert.Equal(esperado, Permissions.CanView(rol, modulo));
    }

    [Theory]
    [MemberData(nameof(MatrizDeGestion))]
    public void CanManage_RespetaLaMatriz(UserRole rol, SigaModule modulo, bool esperado)
    {
        Assert.Equal(esperado, Permissions.CanManage(rol, modulo));
    }

    [Fact]
    public void Contratista_SoloAccedeATareasEHistorial()
    {
        var visibles = Modulos.Where(modulo => Permissions.CanView(UserRole.Contratista, modulo)).ToArray();

        Assert.Equal(new[] { SigaModule.Tareas, SigaModule.Historial }, visibles);
        Assert.False(Permissions.CanManage(UserRole.Contratista, SigaModule.Tareas));
    }

    [Fact]
    public void AdministradorGeneral_EsElUnicoQueGestionaEmpresas()
    {
        Assert.True(Permissions.CanManage(UserRole.AdministradorGeneral, SigaModule.Empresas));

        foreach (var rol in Roles.Where(rol => rol != UserRole.AdministradorGeneral))
        {
            Assert.False(Permissions.CanManage(rol, SigaModule.Empresas));
        }
    }

    [Fact]
    public void AdministradorDeEmpresa_NoGestionaCultivos()
    {
        Assert.True(Permissions.CanManageCultivos(UserRole.AdministradorEmpresa));
        Assert.True(Permissions.CanManageCultivos(UserRole.AdministradorGeneral));
        Assert.True(Permissions.CanManageCultivos(UserRole.IngenieroAgronomo));
        Assert.False(Permissions.CanManageCultivos(UserRole.OperarioCampo));
        Assert.False(Permissions.CanManageCultivos(UserRole.Contratista));
    }

    [Fact]
    public void Tareas_SeparaCrearAsignarEjecutarVerificarYAnular()
    {
        Assert.True(Permissions.CanCreateTarea(UserRole.IngenieroAgronomo));
        Assert.False(Permissions.CanCreateTarea(UserRole.AdministradorEmpresa));
        Assert.False(Permissions.CanCreateTarea(UserRole.OperarioCampo));

        Assert.True(Permissions.CanAssignTarea(UserRole.AdministradorEmpresa));
        Assert.True(Permissions.CanAssignTarea(UserRole.IngenieroAgronomo));
        Assert.False(Permissions.CanAssignTarea(UserRole.OperarioCampo));

        Assert.True(Permissions.CanExecuteTarea(UserRole.OperarioCampo));
        Assert.True(Permissions.CanExecuteTarea(UserRole.Contratista));
        Assert.False(Permissions.CanExecuteTarea(UserRole.IngenieroAgronomo));

        Assert.True(Permissions.CanRegisterResult(UserRole.AdministradorEmpresa));
        Assert.True(Permissions.CanVerifyTarea(UserRole.IngenieroAgronomo));
        Assert.True(Permissions.CanAnularTarea(UserRole.AdministradorEmpresa));
        Assert.False(Permissions.CanAnularTarea(UserRole.OperarioCampo));
    }

    [Fact]
    public void Fotos_SoloLasOcultanAdministradorDeEmpresaEIngeniero()
    {
        Assert.True(Permissions.CanHidePhoto(UserRole.AdministradorEmpresa));
        Assert.True(Permissions.CanHidePhoto(UserRole.IngenieroAgronomo));
        Assert.False(Permissions.CanHidePhoto(UserRole.AdministradorGeneral));
        Assert.False(Permissions.CanHidePhoto(UserRole.OperarioCampo));
        Assert.False(Permissions.CanHidePhoto(UserRole.Contratista));
    }

    private static HashSet<SigaModule> ModulosVisibles(UserRole rol) => rol switch
    {
        UserRole.AdministradorGeneral => [.. Modulos],
        UserRole.AdministradorEmpresa => [.. Modulos],
        UserRole.IngenieroAgronomo =>
        [
            SigaModule.Establecimientos,
            SigaModule.Potreros,
            SigaModule.Campanas,
            SigaModule.Eventos,
            SigaModule.Tareas,
            SigaModule.Historial,
            SigaModule.Catalogos,
            SigaModule.Reportes,
            SigaModule.Insumos,
            SigaModule.Maquinaria
        ],
        UserRole.OperarioCampo =>
        [
            SigaModule.Establecimientos,
            SigaModule.Potreros,
            SigaModule.Campanas,
            SigaModule.Eventos,
            SigaModule.Tareas,
            SigaModule.Historial
        ],
        UserRole.Contratista => [SigaModule.Tareas, SigaModule.Historial],
        _ => []
    };

    private static HashSet<SigaModule> ModulosGestionables(UserRole rol) => rol switch
    {
        UserRole.AdministradorGeneral =>
        [
            SigaModule.Empresas,
            SigaModule.Usuarios,
            SigaModule.Establecimientos,
            SigaModule.Potreros,
            SigaModule.Campanas,
            SigaModule.Catalogos,
            SigaModule.Insumos,
            SigaModule.Maquinaria,
            SigaModule.Contratistas
        ],
        UserRole.AdministradorEmpresa =>
        [
            SigaModule.Usuarios,
            SigaModule.Establecimientos,
            SigaModule.Potreros,
            SigaModule.Campanas,
            SigaModule.Catalogos,
            SigaModule.Insumos,
            SigaModule.Maquinaria,
            SigaModule.Contratistas
        ],
        UserRole.IngenieroAgronomo =>
        [
            SigaModule.Campanas,
            SigaModule.Eventos,
            SigaModule.Tareas
        ],
        UserRole.OperarioCampo => [SigaModule.Eventos],
        UserRole.Contratista => [],
        _ => []
    };
}
