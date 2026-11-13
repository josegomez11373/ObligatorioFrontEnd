namespace ObligatorioFrontEnd.Models;

/// <summary>
/// Archivo de imagen leído desde el navegador. No es un dato de demostración:
/// vive en memoria solo hasta que el adaptador lo envía a la API.
/// </summary>
public sealed record FotoUpload(string NombreArchivo, string ContentType, byte[] Contenido);
