using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Empresa
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Nombre { get; init; } = string.Empty;
    public bool Activa { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
