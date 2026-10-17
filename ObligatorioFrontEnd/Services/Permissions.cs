using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public enum SigaModule
{
    Empresas,
    Establecimientos,
    Potreros,
    Campanas,
    Eventos
}

public static class Permissions
{
    public static bool CanView(UserRole role, SigaModule module) => module switch
    {
        SigaModule.Empresas => role == UserRole.AdministradorGeneral,
        SigaModule.Establecimientos => role is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo or UserRole.OperarioCampo,
        SigaModule.Potreros => role is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo or UserRole.OperarioCampo,
        SigaModule.Campanas => role is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo,
        SigaModule.Eventos => role is UserRole.AdministradorGeneral or UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo or UserRole.OperarioCampo or UserRole.Contratista,
        _ => false
    };

    public static bool CanManage(UserRole role, SigaModule module) => module switch
    {
        SigaModule.Empresas => role == UserRole.AdministradorGeneral,
        SigaModule.Establecimientos => role == UserRole.AdministradorEmpresa,
        SigaModule.Potreros => role == UserRole.AdministradorEmpresa,
        SigaModule.Campanas => role is UserRole.AdministradorEmpresa or UserRole.IngenieroAgronomo,
        SigaModule.Eventos => role is UserRole.IngenieroAgronomo or UserRole.OperarioCampo,
        _ => false
    };
}
