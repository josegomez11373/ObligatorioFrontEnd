using System.ComponentModel.DataAnnotations;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Models;

public sealed class CampanaEditModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "La empresa es obligatoria.")]
    public Guid EmpresaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El cultivo es obligatorio.")]
    public Guid CultivoId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    public DateTime? FechaFin { get; set; }

    public EstadoCampana Estado { get; set; } = EstadoCampana.Planificada;

    public List<Guid> PotreroIds { get; set; } = [];

    public long Version { get; set; }
}
