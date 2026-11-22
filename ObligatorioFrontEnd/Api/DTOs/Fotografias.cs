namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Metadatos de una fotografía tal como los devuelve la API.</summary>
public sealed record FotografiaDto(
    Guid Id,
    Guid EventoId,
    DateTime FechaCaptura,
    double? Latitud,
    double? Longitud,
    string Archivo,
    bool Visible,
    string? MotivoOcultamiento,
    DateTime FechaCreacion,
    long Version)
{
    /// <summary>Nombre del archivo almacenado, sin la ruta completa.</summary>
    public string NombreArchivo => Path.GetFileName(Archivo);
}

public sealed class OcultarFotografiaDto
{
    public string Motivo { get; set; } = string.Empty;
    public long Version { get; set; }
}
