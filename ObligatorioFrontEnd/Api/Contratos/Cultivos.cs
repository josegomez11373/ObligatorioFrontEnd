namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Espejo de ApiSiga.Contracts.Cultivos.CultivoDto.</summary>
public sealed record CultivoDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    bool Activo,
    DateTime FechaCreacion,
    long Version);

/// <summary>Espejo de ApiSiga.Contracts.Cultivos.CrearCultivoDto.</summary>
public sealed class CrearCultivoDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
}

/// <summary>Espejo de ApiSiga.Contracts.Cultivos.ActualizarCultivoDto.</summary>
public sealed class ActualizarCultivoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
