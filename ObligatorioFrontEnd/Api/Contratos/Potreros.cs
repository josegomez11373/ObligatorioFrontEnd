namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Espejo de ApiSiga.Contracts.Potreros.PotreroDto.</summary>
public sealed record PotreroDto(
    Guid Id,
    Guid EmpresaId,
    Guid EstablecimientoId,
    string EstablecimientoNombre,
    string Nombre,
    double? SuperficieHa,
    string? Limites,
    bool Activo,
    DateTime FechaCreacion,
    long Version);

/// <summary>Espejo de ApiSiga.Contracts.Potreros.CrearPotreroDto.</summary>
public sealed class CrearPotreroDto
{
    public Guid EstablecimientoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public double? SuperficieHa { get; set; }
    public string? Limites { get; set; }
}

/// <summary>Espejo de ApiSiga.Contracts.Potreros.ActualizarPotreroDto.</summary>
public sealed class ActualizarPotreroDto
{
    public Guid Id { get; set; }
    public Guid EstablecimientoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public double? SuperficieHa { get; set; }
    public string? Limites { get; set; }
    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
