using System.ComponentModel.DataAnnotations;

namespace ObligatorioFrontEnd.Models;

/// <summary>Datos del formulario de alta/edición de un evento.</summary>
public sealed class EventoFormModel
{
    public Guid Id { get; set; }
    public Guid CampanaPotreroId { get; set; }
    public Guid TipoEventoId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;

    public string? Ubicacion { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public long Version { get; set; }
    public IReadOnlyList<FotoUpload> Fotos { get; set; } = [];
}
