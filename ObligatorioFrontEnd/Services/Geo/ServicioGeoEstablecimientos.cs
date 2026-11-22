using System.Text.Encodings.Web;
using System.Text.Json;
using ObligatorioFrontEnd.Models.Geo;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Persiste las coordenadas en <c>Data/establecimientos-geo.json</c>. El archivo
/// es compartido por todos los circuitos del servidor, por eso el semáforo es
/// estático y la lectura va siempre a disco (evita cachés desactualizadas).
/// </summary>
public sealed class ServicioGeoEstablecimientos : IServicioGeoEstablecimientos
{
    private static readonly SemaphoreSlim Escritura = new(1, 1);

    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IWebHostEnvironment _entorno;
    private readonly NotificadorDatos _notificador;
    private readonly ILogger<ServicioGeoEstablecimientos> _logger;

    public ServicioGeoEstablecimientos(
        IWebHostEnvironment entorno,
        NotificadorDatos notificador,
        ILogger<ServicioGeoEstablecimientos> logger)
    {
        _entorno = entorno;
        _notificador = notificador;
        _logger = logger;
    }

    private string Ruta => Path.Combine(_entorno.ContentRootPath, "Data", "establecimientos-geo.json");

    public async Task<IReadOnlyList<CoordenadaEstablecimiento>> GetTodasAsync(
        CancellationToken cancellationToken = default)
    {
        var lista = await LeerAsync(cancellationToken);
        return lista.Where(coordenada => coordenada.EsValida).ToList();
    }

    public async Task<CoordenadaEstablecimiento> GuardarAsync(
        Guid establecimientoId,
        double latitud,
        double longitud,
        CancellationToken cancellationToken = default)
    {
        if (establecimientoId == Guid.Empty)
        {
            throw new InvalidOperationException("El establecimiento es obligatorio.");
        }

        if (!double.IsFinite(latitud) || latitud is < -90 or > 90)
        {
            throw new InvalidOperationException("La latitud debe estar entre -90 y 90.");
        }

        if (!double.IsFinite(longitud) || longitud is < -180 or > 180)
        {
            throw new InvalidOperationException("La longitud debe estar entre -180 y 180.");
        }

        var coordenada = new CoordenadaEstablecimiento
        {
            EstablecimientoId = establecimientoId,
            Latitud = latitud,
            Longitud = longitud,
            ActualizadaUtc = DateTime.UtcNow
        };

        await Escritura.WaitAsync(cancellationToken);

        try
        {
            var lista = await LeerAsync(cancellationToken);
            var indice = lista.FindIndex(
                existente => existente.EstablecimientoId == establecimientoId);

            if (indice >= 0)
            {
                lista[indice] = coordenada;
            }
            else
            {
                lista.Add(coordenada);
            }

            await EscribirAsync(lista, cancellationToken);
        }
        finally
        {
            Escritura.Release();
        }

        _notificador.Notificar(ModulosDatos.Mapa);
        return coordenada;
    }

    public async Task<bool> EliminarAsync(
        Guid establecimientoId,
        CancellationToken cancellationToken = default)
    {
        await Escritura.WaitAsync(cancellationToken);

        try
        {
            var lista = await LeerAsync(cancellationToken);
            var eliminadas = lista.RemoveAll(
                coordenada => coordenada.EstablecimientoId == establecimientoId);

            if (eliminadas == 0)
            {
                return false;
            }

            await EscribirAsync(lista, cancellationToken);
        }
        finally
        {
            Escritura.Release();
        }

        _notificador.Notificar(ModulosDatos.Mapa);
        return true;
    }

    private async Task<List<CoordenadaEstablecimiento>> LeerAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(Ruta))
        {
            return [];
        }

        try
        {
            await using var archivo = File.OpenRead(Ruta);
            return await JsonSerializer.DeserializeAsync<List<CoordenadaEstablecimiento>>(
                archivo, Opciones, cancellationToken) ?? [];
        }
        catch (JsonException exception)
        {
            // Un archivo corrupto no debe tumbar el mapa: se ignora y se avisa.
            _logger.LogWarning(exception, "El archivo de coordenadas {Ruta} no es válido.", Ruta);
            return [];
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "No se pudo leer el archivo de coordenadas {Ruta}.", Ruta);
            return [];
        }
    }

    private async Task EscribirAsync(
        List<CoordenadaEstablecimiento> lista,
        CancellationToken cancellationToken)
    {
        try
        {
            var carpeta = Path.GetDirectoryName(Ruta);
            if (!string.IsNullOrWhiteSpace(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            var temporal = $"{Ruta}.tmp";
            await using (var archivo = File.Create(temporal))
            {
                await JsonSerializer.SerializeAsync(archivo, lista, Opciones, cancellationToken);
                await archivo.FlushAsync(cancellationToken);
            }

            File.Move(temporal, Ruta, overwrite: true);
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "No se pudo escribir el archivo de coordenadas {Ruta}.", Ruta);
            throw new InvalidOperationException(
                "No se pudo guardar la ubicación en el servidor. Revisá los permisos de la carpeta Data.", exception);
        }
    }
}
