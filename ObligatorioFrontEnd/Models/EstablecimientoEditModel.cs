using System;
using System.ComponentModel.DataAnnotations;

namespace ObligatorioFrontEnd.Models;

public sealed class EstablecimientoEditModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "La empresa es obligatoria.")]
    public Guid EmpresaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Superficie inválida.")]
    public double? SuperficieHa { get; set; }

    [StringLength(200, ErrorMessage = "La ubicación no puede superar 200 caracteres.")]
    public string? Ubicacion { get; set; }

    [StringLength(500, ErrorMessage = "Los responsables no pueden superar 500 caracteres.")]
    public string? Responsables { get; set; }

    public bool Activo { get; set; } = true;
    public long Version { get; set; }
}
