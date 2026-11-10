namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Espejo de ApiSiga.Contracts.Empresas.EmpresaDto.</summary>
public sealed record EmpresaDto(
    Guid Id,
    string Nombre,
    string? RazonSocial,
    bool Activo,
    DateTime FechaCreacion,
    long Version);

/// <summary>Espejo de ApiSiga.Contracts.Empresas.CrearEmpresaDto.</summary>
public sealed class CrearEmpresaDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
}

/// <summary>Espejo de ApiSiga.Contracts.Empresas.ActualizarEmpresaDto.</summary>
public sealed class ActualizarEmpresaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
