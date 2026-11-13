using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Tipos de tarea contra ApiSiga.</summary>
public sealed class ServicioTiposTareaApi : IServicioTiposTarea
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioTiposTareaApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<TipoTareaDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/tipos-tarea?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<TipoTareaDto>>(ruta, cancellationToken);
    }

    public async Task<TipoTareaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<TipoTareaDto>($"/api/v1/tipos-tarea/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<TipoTareaDto> CreateAsync(
        CrearTipoTareaDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = await _api.PostAsync<TipoTareaDto>(
            "/api/v1/tipos-tarea", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.TiposTarea);
        return creado;
    }

    public async Task<TipoTareaDto?> UpdateAsync(
        ActualizarTipoTareaDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizado = await _api.PutAsync<TipoTareaDto>(
                $"/api/v1/tipos-tarea/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.TiposTarea);
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
            await _api.DeleteAsync($"/api/v1/tipos-tarea/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.TiposTarea);
        return true;
    }
}
