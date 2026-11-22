using System.Collections.Concurrent;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Caché de respuestas GET por circuito. Evita repetir el mismo listado cuando
/// varias páginas o servicios lo piden casi al mismo tiempo (por ejemplo, el
/// tablero de reportes y los catálogos del filtro).
/// Cualquier operación de escritura limpia la caché completa.
/// </summary>
public sealed class CacheDatos
{
    private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(20);

    private readonly ConcurrentDictionary<string, Entrada> _entradas = new(StringComparer.Ordinal);

    public bool TryObtener<T>(string clave, out T valor)
    {
        valor = default!;

        if (!_entradas.TryGetValue(clave, out var entrada))
        {
            return false;
        }

        if (entrada.ExpiraUtc <= DateTime.UtcNow)
        {
            _entradas.TryRemove(clave, out _);
            return false;
        }

        if (entrada.Valor is not T encontrado)
        {
            return false;
        }

        valor = encontrado;
        return true;
    }

    public void Guardar<T>(string clave, T valor)
    {
        if (valor is null)
        {
            return;
        }

        _entradas[clave] = new Entrada(valor, DateTime.UtcNow.Add(Duracion));
    }

    public void Limpiar() => _entradas.Clear();

    private readonly record struct Entrada(object Valor, DateTime ExpiraUtc);
}
