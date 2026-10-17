using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Maquinaria
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public decimal? CostoHora { get; init; }
    public bool Activo { get; init; } = true;
}
