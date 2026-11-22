namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Contrato esperado para los tipos de tarea de una empresa.</summary>
public sealed record TipoTareaDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    bool Activo,
    DateTime FechaCreacion,
    long Version);

public sealed class CrearTipoTareaDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
}

public sealed class ActualizarTipoTareaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
