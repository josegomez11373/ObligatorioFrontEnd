using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Implementación de Potreros contra ApiSiga.</summary>
public sealed class ServicioPotrerosApi : IServicioPotreros
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioPotrerosApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<PotreroDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/potreros?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<PotreroDto>>(ruta, cancellationToken);
    }

    public async Task<PotreroDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<PotreroDto>($"/api/v1/potreros/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<PotreroDto> CreateAsync(
        CrearPotreroDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = await _api.PostAsync<PotreroDto>(
            "/api/v1/potreros", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Potreros);
        return creado;
    }

    public async Task<PotreroDto?> UpdateAsync(
        ActualizarPotreroDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizado = await _api.PutAsync<PotreroDto>(
                $"/api/v1/potreros/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Potreros);
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
            await _api.DeleteAsync($"/api/v1/potreros/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Potreros);
        return true;
    }
}
