using System;
using System.ComponentModel.DataAnnotations;

namespace ObligatorioFrontEnd.Models;

public sealed class EmpresaEditModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
