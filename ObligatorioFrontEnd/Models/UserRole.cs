namespace ObligatorioFrontEnd.Models;

public enum UserRole
{
    AdministradorGeneral,
    AdministradorEmpresa,
    IngenieroAgronomo,
    OperarioCampo,
    Contratista
}

public static class UserRoleExtensions
{
    public static string DisplayName(this UserRole role) => role switch
    {
        UserRole.AdministradorGeneral => "Administrador general",
        UserRole.AdministradorEmpresa => "Administrador de empresa",
        UserRole.IngenieroAgronomo => "Ingeniero agrónomo",
        UserRole.OperarioCampo => "Operario de campo",
        UserRole.Contratista => "Contratista",
        _ => role.ToString()
    };
}
