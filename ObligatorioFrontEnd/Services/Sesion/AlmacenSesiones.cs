using System.Collections.Concurrent;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Datos de una sesión que se guardan en el servidor. El navegador solo recibe
/// un identificador opaco; el JWT nunca sale del proceso.
/// </summary>
public sealed class SesionGuardada
{
    public string? Token { get; init; }
    public DateTime Expira { get; init; }

    /// <summary>Usuario devuelto por la API cuando el login fue real.</summary>
    public UsuarioSesionDto? Usuario { get; init; }

    /// <summary>Usuario del modo demo, para no depender de volver a buscarlo.</summary>
    public DemoUser? UsuarioDemo { get; init; }
}

/// <summary>
/// Almacén en memoria compartido por todos los circuitos del proceso. La clave
/// viaja en <c>ProtectedSessionStorage</c>, así que sobrevive a un F5 pero no al
/// cierre del navegador ni al reinicio del servidor.
/// </summary>
public sealed class AlmacenSesiones
{
    private readonly ConcurrentDictionary<string, SesionGuardada> _sesiones = new(StringComparer.Ordinal);

    public string Guardar(SesionGuardada sesion)
    {
        var id = Guid.NewGuid().ToString("N");
        _sesiones[id] = sesion;
        return id;
    }

    public SesionGuardada? Obtener(string id) =>
        _sesiones.TryGetValue(id, out var sesion) ? sesion : null;

    public void Eliminar(string id) => _sesiones.TryRemove(id, out _);
}

public static class SesionKeys
{
    public const string Sesion = "siga.sesion";
    public const string EmpresaActiva = "siga.empresa";
}
