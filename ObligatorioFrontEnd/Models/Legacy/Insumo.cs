using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Insumo
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public string Nombre { get; set; } = string.Empty;
    public string? Unidad { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public bool Activo { get; set; } = true;
}
