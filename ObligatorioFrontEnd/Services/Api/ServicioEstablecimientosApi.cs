using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Implementación de Establecimientos contra ApiSiga.</summary>
public sealed class ServicioEstablecimientosApi : IServicioEstablecimientos
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioEstablecimientosApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<EstablecimientoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/establecimientos?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<EstablecimientoDto>>(ruta, cancellationToken);
    }

    public async Task<EstablecimientoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<EstablecimientoDto>(
                $"/api/v1/establecimientos/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<EstablecimientoDto> CreateAsync(
        CrearEstablecimientoDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = await _api.PostAsync<EstablecimientoDto>(
            "/api/v1/establecimientos", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Establecimientos);
        return creado;
    }

    public async Task<EstablecimientoDto?> UpdateAsync(
        ActualizarEstablecimientoDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizado = await _api.PutAsync<EstablecimientoDto>(
                $"/api/v1/establecimientos/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Establecimientos);
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
            await _api.DeleteAsync($"/api/v1/establecimientos/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Establecimientos);
        return true;
    }
}
