namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Contrato esperado para los contratistas de una empresa.</summary>
public sealed record ContratistaDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    Guid? UsuarioId,
    string? UsuarioNombre,
    string? UsuarioEmail,
    bool Activo,
    DateTime FechaCreacion,
    long Version);

public sealed class CrearContratistaDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public Guid? UsuarioId { get; set; }
}

public sealed class ActualizarContratistaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid? UsuarioId { get; set; }
    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
