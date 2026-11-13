namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>
/// Evento de campo tal como lo devuelve la API. La campaña se deduce cruzando
/// <see cref="CampanaPotreroId"/> con <see cref="CampanaDto.Potreros"/>.
/// </summary>
public sealed record EventoDto(
    Guid Id,
    Guid EmpresaId,
    Guid CampanaPotreroId,
    string? PotreroNombre,
    Guid TipoEventoId,
    string? TipoEventoNombre,
    DateTime Fecha,
    string? Descripcion,
    string? Ubicacion,
    double? Latitud,
    double? Longitud,
    Guid? RegistradoPorId,
    string? RegistradoPor,
    EstadoEvento Estado,
    string? MotivoAnulacion,
    DateTime FechaCreacion,
    long Version)
{
    private readonly IReadOnlyList<FotografiaDto>? fotografias;

    /// <summary>
    /// La API solo incluye las fotografías visibles. Se normaliza a vacía si
    /// omite o envía <c>null</c>.
    /// </summary>
    public IReadOnlyList<FotografiaDto> Fotografias
    {
        get => fotografias ?? [];
        init => fotografias = value;
    }
}

public sealed class CrearEventoDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public Guid CampanaPotreroId { get; set; }
    public Guid TipoEventoId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow.Date;
    public string? Descripcion { get; set; } = string.Empty;
    public string? Ubicacion { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}

public sealed class ActualizarEventoDto
{
    public Guid Id { get; set; }
    public Guid TipoEventoId { get; set; }
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; } = string.Empty;
    public string? Ubicacion { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public long Version { get; set; }
}

/// <summary>Alta del mismo fenómeno en varios potreros (un evento por potrero).</summary>
public sealed class CrearEventosLoteDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public IReadOnlyCollection<Guid> CampanaPotreroIds { get; set; } = [];
    public Guid TipoEventoId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow.Date;
    public string? Descripcion { get; set; } = string.Empty;
    public string? Ubicacion { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}
