using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Fotografia
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EventoId { get; init; }
    public string NombreArchivo { get; init; } = string.Empty;
    public string? MotivoOcultamiento { get; init; }
    public bool Visible { get; init; } = true;
}
