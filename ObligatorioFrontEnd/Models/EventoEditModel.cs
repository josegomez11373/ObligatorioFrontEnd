using System.ComponentModel.DataAnnotations;

namespace ObligatorioFrontEnd.Models;

public sealed class EventoEditModel
{
    public Guid Id { get; set; }
    public Guid CampanaPotreroId { get; set; }
    public Guid TipoEventoId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(1000, ErrorMessage = "La descripción no puede superar 1000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La ubicación no puede superar 200 caracteres.")]
    public string? Ubicacion { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public List<string> FotoNombres { get; set; } = [];
    public List<FotoUpload> Fotos { get; set; } = [];
    public long Version { get; set; }
}
