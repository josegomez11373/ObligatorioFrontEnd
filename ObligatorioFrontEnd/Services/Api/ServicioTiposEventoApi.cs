using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Tipos de evento contra ApiSiga.</summary>
public sealed class ServicioTiposEventoApi : IServicioTiposEvento
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioTiposEventoApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<TipoEventoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/tipos-evento?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<TipoEventoDto>>(ruta, cancellationToken);
    }

    public async Task<TipoEventoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<TipoEventoDto>($"/api/v1/tipos-evento/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<TipoEventoDto> CreateAsync(
        CrearTipoEventoDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = await _api.PostAsync<TipoEventoDto>(
            "/api/v1/tipos-evento", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.TiposEvento);
        return creado;
    }

    public async Task<TipoEventoDto?> UpdateAsync(
        ActualizarTipoEventoDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizado = await _api.PutAsync<TipoEventoDto>(
                $"/api/v1/tipos-evento/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.TiposEvento);
            return actualizado;
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.DeleteAsync($"/api/v1/tipos-evento/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.TiposEvento);
        return true;
    }
}
