using System;

namespace ObligatorioFrontEnd.Models;

public sealed record TipoEvento
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Nombre { get; init; } = string.Empty;
    public bool Activo { get; init; } = true;
}
