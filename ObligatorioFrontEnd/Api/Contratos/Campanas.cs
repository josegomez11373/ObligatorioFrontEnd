namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Espejo de ApiSiga.Contracts.Seguridad.EstadoCampana.</summary>
public enum EstadoCampana
{
    Planificada,
    Activa,
    Cerrada,
    Anulada
}

public static class EstadoCampanaExtensions
{
    public static string DisplayName(this EstadoCampana estado) => estado switch
    {
        EstadoCampana.Planificada => "Planificada",
        EstadoCampana.Activa => "Activa",
        EstadoCampana.Cerrada => "Cerrada",
        EstadoCampana.Anulada => "Anulada",
        _ => estado.ToString()
    };
}

/// <summary>Espejo de ApiSiga.Contracts.Campanas.CampanaPotreroDto.</summary>
public sealed record CampanaPotreroDto(
    Guid Id,
    Guid PotreroId,
    string PotreroNombre,
    DateTime FechaInicio,
    DateTime? FechaFin,
    EstadoCampana Estado,
    long Version);

/// <summary>Espejo de ApiSiga.Contracts.Campanas.CampanaDto.</summary>
public sealed record CampanaDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    Guid CultivoId,
    string CultivoNombre,
    DateTime FechaInicio,
    DateTime? FechaFin,
    EstadoCampana Estado,
    DateTime FechaCreacion,
    long Version)
{
    private readonly IReadOnlyList<CampanaPotreroDto>? potreros;

    /// <summary>
    /// La API puede omitir o enviar <c>null</c> en la colección; se normaliza a
    /// vacía para que las pantallas muestren "sin potreros" en vez de fallar.
    /// </summary>
    public IReadOnlyList<CampanaPotreroDto> Potreros
    {
        get => potreros ?? [];
        init => potreros = value;
    }
}

/// <summary>Espejo de ApiSiga.Contracts.Campanas.CrearCampanaDto.</summary>
public sealed class CrearCampanaDto
{
    /// <summary>Solo lo usa el administrador general. Para el resto se toma del token.</summary>
    public Guid? EmpresaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public Guid CultivoId { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow.Date;
    public DateTime? FechaFin { get; set; }
    public List<Guid> PotreroIds { get; set; } = [];
}

/// <summary>Espejo de ApiSiga.Contracts.Campanas.ActualizarCampanaDto.</summary>
public sealed class ActualizarCampanaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid CultivoId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public EstadoCampana Estado { get; set; } = EstadoCampana.Planificada;

    /// <summary>
    /// Si viene <c>null</c> se conservan los potreros actuales. Si viene una
    /// lista, reemplaza la selección anterior.
    /// </summary>
    public List<Guid>? PotreroIds { get; set; }

    public long Version { get; set; }
}

/// <summary>Espejo de ApiSiga.Contracts.Campanas.CerrarCampanaPotreroDto.</summary>
public sealed class CerrarCampanaPotreroDto
{
    public DateTime? FechaFin { get; set; }
    public long? Version { get; set; }
}
