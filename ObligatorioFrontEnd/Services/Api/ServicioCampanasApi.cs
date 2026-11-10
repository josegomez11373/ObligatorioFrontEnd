using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Implementación de Campañas contra ApiSiga.</summary>
public sealed class ServicioCampanasApi : IServicioCampanas
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioCampanasApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<CampanaDto>> GetAllAsync(
        bool incluirAnuladas = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/campanas?incluirAnuladas={incluirAnuladas.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<CampanaDto>>(ruta, cancellationToken);
    }

    public async Task<CampanaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<CampanaDto>($"/api/v1/campanas/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<CampanaDto> CreateAsync(
        CrearCampanaDto dto,
        CancellationToken cancellationToken = default)
    {
        var creada = await _api.PostAsync<CampanaDto>(
            "/api/v1/campanas", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Campanas);
        return creada;
    }

    public async Task<CampanaDto?> UpdateAsync(
        ActualizarCampanaDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizada = await _api.PutAsync<CampanaDto>(
                $"/api/v1/campanas/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Campanas);
            return actualizada;
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> AnularAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.DeleteAsync($"/api/v1/campanas/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Campanas);
        return true;
    }
}
