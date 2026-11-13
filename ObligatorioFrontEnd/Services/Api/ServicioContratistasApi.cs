using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Contratistas contra ApiSiga.</summary>
public sealed class ServicioContratistasApi : IServicioContratistas
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioContratistasApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<ContratistaDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/contratistas?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<ContratistaDto>>(ruta, cancellationToken);
    }

    public async Task<ContratistaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<ContratistaDto>($"/api/v1/contratistas/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<ContratistaDto> CreateAsync(
        CrearContratistaDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = await _api.PostAsync<ContratistaDto>(
            "/api/v1/contratistas", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Contratistas);
        return creado;
    }

    public async Task<ContratistaDto?> UpdateAsync(
        ActualizarContratistaDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizado = await _api.PutAsync<ContratistaDto>(
                $"/api/v1/contratistas/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Contratistas);
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
            await _api.DeleteAsync($"/api/v1/contratistas/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Contratistas);
        return true;
    }
}
