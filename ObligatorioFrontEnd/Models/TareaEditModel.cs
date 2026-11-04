using System.ComponentModel.DataAnnotations;

namespace ObligatorioFrontEnd.Models;

public sealed class TareaEditModel
{
    public Guid Id { get; set; }
    public Guid CampanaPotreroId { get; set; }
    public Guid? EventoId { get; set; }
    public Guid TipoTareaId { get; set; }
    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaPlanificada { get; set; } = DateTime.Today;
    public decimal? Costo { get; set; }
    public TareaEstado Estado { get; set; } = TareaEstado.Planificada;
    public long Version { get; set; }
}
