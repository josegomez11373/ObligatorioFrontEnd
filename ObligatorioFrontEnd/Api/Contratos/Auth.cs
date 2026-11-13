namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Espejo de ApiSiga.Contracts.Auth.IniciarSesionDto.</summary>
public sealed class IniciarSesionDto
{
    public string Email { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

/// <summary>Espejo de ApiSiga.Contracts.Seguridad.RolUsuario.</summary>
public enum RolApi
{
    AdministradorGeneral,
    AdministradorEmpresa,
    IngenieroAgronomo,
    OperarioCampo,
    Contratista
}

/// <summary>Espejo de ApiSiga.Contracts.Auth.UsuarioSesionDto.</summary>
public sealed record UsuarioSesionDto(
    Guid Id,
    string Nombre,
    string Email,
    RolApi Rol,
    Guid? EmpresaId,
    string? EmpresaNombre,
    string EtiquetaAlcance)
{
    private readonly IReadOnlyList<Guid>? establecimientoIds;

    /// <summary>Se normaliza a vacía si la API omite o envía <c>null</c>.</summary>
    public IReadOnlyList<Guid> EstablecimientoIds
    {
        get => establecimientoIds ?? [];
        init => establecimientoIds = value;
    }
}

/// <summary>Espejo de ApiSiga.Contracts.Auth.RespuestaInicioSesionDto.</summary>
public sealed record RespuestaInicioSesionDto(
    string Token,
    DateTime Expira,
    UsuarioSesionDto Usuario);
