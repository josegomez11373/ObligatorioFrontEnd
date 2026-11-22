namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Contrato esperado para los insumos de una empresa.</summary>
public sealed record InsumoDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    string Unidad,
    decimal? PrecioUnitario,
    bool Activo,
    DateTime FechaCreacion,
    long Version);

public sealed class CrearInsumoDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = "unidad";
    public decimal? PrecioUnitario { get; set; }
}

public sealed class ActualizarInsumoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = "unidad";
    public decimal? PrecioUnitario { get; set; }
    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
