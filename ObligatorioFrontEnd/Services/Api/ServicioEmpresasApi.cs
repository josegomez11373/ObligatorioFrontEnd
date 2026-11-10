using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Implementación de Empresas contra ApiSiga.</summary>
public sealed class ServicioEmpresasApi : IServicioEmpresas
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioEmpresasApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<EmpresaDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/empresas?incluirInactivas={includeInactive.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<EmpresaDto>>(ruta, cancellationToken);
    }

    public async Task<EmpresaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<EmpresaDto>($"/api/v1/empresas/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<EmpresaDto> CreateAsync(
        string nombre,
        CancellationToken cancellationToken = default)
    {
        var creada = await _api.PostAsync<EmpresaDto>(
            "/api/v1/empresas",
            new CrearEmpresaDto { Nombre = nombre },
            cancellationToken);

        _notificador.Notificar(ModulosDatos.Empresas);
        return creada;
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        string nombre,
        bool activo,
        long expectedVersion = 0,
        CancellationToken cancellationToken = default)
    {
        EmpresaDto actual;

        try
        {
            actual = await _api.GetAsync<EmpresaDto>($"/api/v1/empresas/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        // La UI usa 0 = "no validar versión"; la API siempre compara.
        var version = expectedVersion == 0 ? actual.Version : expectedVersion;

        try
        {
            await _api.PutAsync<EmpresaDto>(
                $"/api/v1/empresas/{id}",
                new ActualizarEmpresaDto
                {
                    Id = id,
                    Nombre = nombre,
                    // La pantalla todavía no edita la razón social: se conserva la de la API.
                    RazonSocial = actual.RazonSocial,
                    Activo = activo,
                    Version = version
                },
                cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Empresas);
        return true;
    }

    public async Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.DeleteAsync($"/api/v1/empresas/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Empresas);
        return true;
    }
}
