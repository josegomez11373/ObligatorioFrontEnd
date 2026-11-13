using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Maquinaria contra ApiSiga.</summary>
public sealed class ServicioMaquinariaApi : IServicioMaquinaria
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioMaquinariaApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<MaquinariaDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/maquinaria?incluirInactivas={incluirInactivos.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<MaquinariaDto>>(ruta, cancellationToken);
    }

    public async Task<MaquinariaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<MaquinariaDto>($"/api/v1/maquinaria/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<MaquinariaDto> CreateAsync(
        CrearMaquinariaDto dto,
        CancellationToken cancellationToken = default)
    {
        var creada = await _api.PostAsync<MaquinariaDto>(
            "/api/v1/maquinaria", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Maquinaria);
        return creada;
    }

    public async Task<MaquinariaDto?> UpdateAsync(
        ActualizarMaquinariaDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizada = await _api.PutAsync<MaquinariaDto>(
                $"/api/v1/maquinaria/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Maquinaria);
            return actualizada;
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
            await _api.DeleteAsync($"/api/v1/maquinaria/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Maquinaria);
        return true;
    }
}
