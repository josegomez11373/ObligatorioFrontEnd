using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Insumo
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Unidad { get; init; }
    public decimal? PrecioUnitario { get; init; }
    public bool Activo { get; init; } = true;
}
