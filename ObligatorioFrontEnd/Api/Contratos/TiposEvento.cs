namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Contrato esperado para los tipos de evento de una empresa.</summary>
public sealed record TipoEventoDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    bool Activo,
    DateTime FechaCreacion,
    long Version);

public sealed class CrearTipoEventoDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
}

public sealed class ActualizarTipoEventoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
