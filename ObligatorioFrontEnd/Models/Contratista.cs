using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Contratista
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool Activo { get; init; } = true;
}
