using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Une el almacén en memoria del servidor con el identificador opaco que vive
/// en <see cref="ProtectedSessionStorage"/>. El navegador nunca recibe el JWT.
/// </summary>
public sealed class SesionPersistencia
{
    private readonly AlmacenSesiones _almacen;
    private readonly ProtectedSessionStorage _storage;
    private string? _id;

    public SesionPersistencia(AlmacenSesiones almacen, ProtectedSessionStorage storage)
    {
        _almacen = almacen;
        _storage = storage;
    }

    public async Task GuardarAsync(SesionGuardada sesion)
    {
        OlvidarLocal();
        _id = _almacen.Guardar(sesion);
        await GuardarIdAsync(_id);
    }

    public async Task<SesionGuardada?> RestaurarAsync()
    {
        var id = await LeerIdAsync();
        if (id is null)
        {
            return null;
        }

        var sesion = _almacen.Obtener(id);
        if (sesion is null)
        {
            // El proceso se reinició o la entrada ya no existe.
            await BorrarIdAsync();
            return null;
        }

        _id = id;
        return sesion;
    }

    public async Task OlvidarAsync()
    {
        OlvidarLocal();
        await BorrarIdAsync();
    }

    /// <summary>
    /// Descarta la copia en memoria sin tocar el almacenamiento del navegador.
    /// Se usa cuando la API invalida el token (401) y el próximo restore limpiará
    /// el identificador.
    /// </summary>
    public void OlvidarLocal()
    {
        if (_id is null)
        {
            return;
        }

        _almacen.Eliminar(_id);
        _id = null;
    }

    private async Task GuardarIdAsync(string id)
    {
        try
        {
            await _storage.SetAsync(SesionKeys.Sesion, id);
        }
        catch
        {
            // Sin JS disponible (pruebas de integración) se ignora.
        }
    }

    private async Task<string?> LeerIdAsync()
    {
        try
        {
            var resultado = await _storage.GetAsync<string>(SesionKeys.Sesion);
            return resultado.Success ? resultado.Value : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task BorrarIdAsync()
    {
        try
        {
            await _storage.DeleteAsync(SesionKeys.Sesion);
        }
        catch
        {
            // Sin JS disponible se ignora.
        }
    }
}
