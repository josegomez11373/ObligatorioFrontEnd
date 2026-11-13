namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Espejo de ApiSiga.Contracts.Usuarios.EstablecimientoAsignadoDto.</summary>
public sealed record EstablecimientoAsignadoDto(Guid Id, string Nombre);

/// <summary>Espejo de ApiSiga.Contracts.Usuarios.UsuarioDto.</summary>
public sealed record UsuarioDto(
    Guid Id,
    string Nombre,
    string Email,
    RolApi Rol,
    bool Activo,
    Guid? EmpresaId,
    string? EmpresaNombre,
    DateTime FechaCreacion,
    long Version)
{
    private readonly IReadOnlyList<EstablecimientoAsignadoDto>? establecimientos;

    /// <summary>Se normaliza a vacía si la API omite o envía <c>null</c>.</summary>
    public IReadOnlyList<EstablecimientoAsignadoDto> Establecimientos
    {
        get => establecimientos ?? [];
        init => establecimientos = value;
    }
}

/// <summary>Espejo de ApiSiga.Contracts.Usuarios.CrearUsuarioDto.</summary>
public sealed class CrearUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public RolApi? Rol { get; set; }

    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public List<Guid> EstablecimientoIds { get; set; } = [];
}

/// <summary>Espejo de ApiSiga.Contracts.Usuarios.ActualizarUsuarioDto.</summary>
public sealed class ActualizarUsuarioDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public RolApi? Rol { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Solo lo usa el administrador general.</summary>
    public Guid? EmpresaId { get; set; }

    public List<Guid> EstablecimientoIds { get; set; } = [];
    public long Version { get; set; }
}

/// <summary>Espejo de ApiSiga.Contracts.Usuarios.CambiarContrasenaDto.</summary>
public sealed class CambiarContrasenaDto
{
    public string Contrasena { get; set; } = string.Empty;
}

public static class RolApiExtensions
{
    public static string DisplayName(this RolApi rol) => rol switch
    {
        RolApi.AdministradorGeneral => "Administrador general",
        RolApi.AdministradorEmpresa => "Administrador de empresa",
        RolApi.IngenieroAgronomo => "Ingeniero agrónomo",
        RolApi.OperarioCampo => "Operario de campo",
        RolApi.Contratista => "Contratista",
        _ => rol.ToString()
    };
}
