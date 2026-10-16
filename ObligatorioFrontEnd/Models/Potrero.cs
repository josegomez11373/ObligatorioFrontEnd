using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Potrero
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EstablecimientoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public double? SuperficieHa { get; init; }
    public bool Activo { get; init; } = true;
}
