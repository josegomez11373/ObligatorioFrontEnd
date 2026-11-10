namespace ObligatorioFrontEnd.Models;

/// <summary>
/// Archivo de imagen ya leído desde el navegador. Se mantiene en memoria el tiempo
/// necesario para que el servicio lo persista y calcule su hash.
/// </summary>
public sealed record FotoUpload(string NombreArchivo, string ContentType, byte[] Contenido);
