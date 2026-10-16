using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Establecimiento
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public double? SuperficieHa { get; init; }
    public bool Activo { get; init; } = true;
}
