using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Fotografia
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public Guid EventoId { get; init; }
    public string NombreArchivo { get; init; } = string.Empty;
    public string? RutaRelativa { get; init; }
    public string? AccessToken { get; init; }
    public string? ContentType { get; init; }
    public long? TamanoBytes { get; init; }
    public string? Autor { get; init; }
    public DateTime FechaCaptura { get; init; }
    public string? Hash { get; init; }
    public double? Latitud { get; init; }
    public double? Longitud { get; init; }
    public string? MotivoOcultamiento { get; init; }
    public bool Visible { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public long Version { get; init; } = 1;
}
