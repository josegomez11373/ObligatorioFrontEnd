using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public enum SigaModule
{
    Empresas,
    Establecimientos,
    Potreros,
    Campanas,
    Eventos,
    Tareas,
    Historial,
    Catalogos,
    Reportes,
    Usuarios,
    Auditoria
}

/// <summary>
/// Matriz de permisos de la sección 4.1 del documento v2. La interfaz usa estos
/// métodos para ocultar opciones, pero cada servicio vuelve a validar la operación.
/// </summary>
public static class Permissions
{
    public static bool CanView(UserRole role, SigaModule module) => module switch
    {
        SigaModule.Empresas => role is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa,
        SigaModule.Usuarios => role is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa,
        SigaModule.Establecimientos => role is not UserRole.Contratista,
        SigaModule.Potreros => role is not UserRole.Contratista,
        SigaModule.Campanas => role is not UserRole.Contratista,
        SigaModule.Eventos => role is not UserRole.Contratista,
        SigaModule.Tareas => true,
        SigaModule.Historial => true,
        SigaModule.Catalogos => role is not UserRole.Contratista,
        SigaModule.Reportes => role is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo,
        SigaModule.Auditoria => role is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa,
        _ => false
    };

    public static bool CanManage(UserRole role, SigaModule module) => module switch
    {
        SigaModule.Empresas => role == UserRole.AdministradorGeneral,
        SigaModule.Usuarios => role is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa,
        SigaModule.Establecimientos => role == UserRole.AdministradorEmpresa,
        SigaModule.Potreros => role == UserRole.AdministradorEmpresa,
        SigaModule.Campanas => role is UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo,
        SigaModule.Eventos => role is UserRole.IngenieroAgronomo or UserRole.OperarioCampo,
        SigaModule.Tareas => CanCreateTarea(role),
        SigaModule.Catalogos => role == UserRole.AdministradorEmpresa,
        _ => false
    };

    public static bool CanManageCultivos(UserRole role) =>
        role is UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo;

    public static bool CanCreateTarea(UserRole role) => role == UserRole.IngenieroAgronomo;

    public static bool CanAssignTarea(UserRole role) =>
        role is UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo;

    public static bool CanExecuteTarea(UserRole role) =>
        role is UserRole.OperarioCampo or UserRole.Contratista;

    public static bool CanRegisterResult(UserRole role) =>
        role is UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo;

    public static bool CanVerifyTarea(UserRole role) =>
        role is UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo;

    public static bool CanAnularTarea(UserRole role) =>
        role is UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo;

    public static bool CanHidePhoto(UserRole role) =>
        role is UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo;
}
