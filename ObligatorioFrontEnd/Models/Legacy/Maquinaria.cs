using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Maquinaria
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public string Nombre { get; set; } = string.Empty;
    public decimal? CostoHora { get; set; }
    public bool Activo { get; set; } = true;
}
