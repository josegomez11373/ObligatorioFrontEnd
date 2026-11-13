namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Contrato esperado para la maquinaria de una empresa.</summary>
public sealed record MaquinariaDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    string? Identificacion,
    decimal? CostoHora,
    bool Activo,
    DateTime FechaCreacion,
    long Version);

public sealed class CrearMaquinariaDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Identificacion { get; set; }
    public decimal? CostoHora { get; set; }
}

public sealed class ActualizarMaquinariaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Identificacion { get; set; }
    public decimal? CostoHora { get; set; }
    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
