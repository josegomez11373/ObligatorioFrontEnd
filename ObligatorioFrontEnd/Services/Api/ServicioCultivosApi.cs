using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Implementación de Cultivos contra ApiSiga.</summary>
public sealed class ServicioCultivosApi : IServicioCultivos
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioCultivosApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<CultivoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/cultivos?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<CultivoDto>>(ruta, cancellationToken);
    }

    public async Task<CultivoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<CultivoDto>($"/api/v1/cultivos/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<CultivoDto> CreateAsync(
        CrearCultivoDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = await _api.PostAsync<CultivoDto>(
            "/api/v1/cultivos", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Cultivos);
        return creado;
    }

    public async Task<CultivoDto?> UpdateAsync(
        ActualizarCultivoDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizado = await _api.PutAsync<CultivoDto>(
                $"/api/v1/cultivos/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Cultivos);
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
            await _api.DeleteAsync($"/api/v1/cultivos/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Cultivos);
        return true;
    }
}
