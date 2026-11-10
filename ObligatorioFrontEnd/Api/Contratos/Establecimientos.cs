namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Espejo de ApiSiga.Contracts.Establecimientos.EstablecimientoDto.</summary>
public sealed record EstablecimientoDto(
    Guid Id,
    Guid EmpresaId,
    string EmpresaNombre,
    string Nombre,
    string? Ubicacion,
    string? Responsables,
    double? SuperficieHa,
    bool Activo,
    DateTime FechaCreacion,
    long Version);

/// <summary>Espejo de ApiSiga.Contracts.Establecimientos.CrearEstablecimientoDto.</summary>
public sealed class CrearEstablecimientoDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Ubicacion { get; set; }

    public string? Responsables { get; set; }

    public double? SuperficieHa { get; set; }
}

/// <summary>Espejo de ApiSiga.Contracts.Establecimientos.ActualizarEstablecimientoDto.</summary>
public sealed class ActualizarEstablecimientoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Ubicacion { get; set; }
    public string? Responsables { get; set; }
    public double? SuperficieHa { get; set; }
    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
