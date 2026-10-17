using System;
using System.ComponentModel.DataAnnotations;

namespace ObligatorioFrontEnd.Models;

public sealed class PotreroEditModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "El establecimiento es obligatorio.")]
    public Guid EstablecimientoId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Superficie inválida.")]
    public double? SuperficieHa { get; set; }

    [StringLength(1000, ErrorMessage = "Los límites no pueden superar 1000 caracteres.")]
    public string? Limites { get; set; }

    public bool Activo { get; set; } = true;
}
