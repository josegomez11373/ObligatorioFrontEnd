using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Insumos contra ApiSiga.</summary>
public sealed class ServicioInsumosApi : IServicioInsumos
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioInsumosApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<InsumoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/insumos?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<InsumoDto>>(ruta, cancellationToken);
    }

    public async Task<InsumoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<InsumoDto>($"/api/v1/insumos/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<InsumoDto> CreateAsync(
        CrearInsumoDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = await _api.PostAsync<InsumoDto>(
            "/api/v1/insumos", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Insumos);
        return creado;
    }

    public async Task<InsumoDto?> UpdateAsync(
        ActualizarInsumoDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizado = await _api.PutAsync<InsumoDto>(
                $"/api/v1/insumos/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Insumos);
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
            await _api.DeleteAsync($"/api/v1/insumos/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Insumos);
        return true;
    }
}
