using System;

namespace ObligatorioFrontEnd.Models;

public sealed record Contratista
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EmpresaId { get; init; }
    public Guid? UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
