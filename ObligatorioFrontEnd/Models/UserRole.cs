namespace ObligatorioFrontEnd.Models;

public enum UserRole
{
    AdministradorGeneral,
    AdministradorEmpresa,
    IngenieroAgronomo,
    OperarioCampo
}

public static class UserRoleExtensions
{
    public static string DisplayName(this UserRole role) => role switch
    {
        UserRole.AdministradorGeneral => "Administrador general",
        UserRole.AdministradorEmpresa => "Administrador de empresa",
        UserRole.IngenieroAgronomo => "Ingeniero agrónomo",
        UserRole.OperarioCampo => "Operario de campo",
        _ => role.ToString()
    };
}
