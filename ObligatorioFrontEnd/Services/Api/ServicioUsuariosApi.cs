using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Implementación de Usuarios contra ApiSiga.</summary>
public sealed class ServicioUsuariosApi : IServicioUsuarios
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioUsuariosApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<UsuarioDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var ruta = $"/api/v1/usuarios?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";
        return await _api.GetAsync<List<UsuarioDto>>(ruta, cancellationToken);
    }

    public async Task<UsuarioDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<UsuarioDto>($"/api/v1/usuarios/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<UsuarioDto> CreateAsync(
        CrearUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = await _api.PostAsync<UsuarioDto>(
            "/api/v1/usuarios", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Usuarios);
        return creado;
    }

    public async Task<UsuarioDto?> UpdateAsync(
        ActualizarUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizado = await _api.PutAsync<UsuarioDto>(
                $"/api/v1/usuarios/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Usuarios);
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
            await _api.DeleteAsync($"/api/v1/usuarios/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Usuarios);
        return true;
    }

    public async Task<UsuarioDto?> CambiarContrasenaAsync(
        Guid id,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.PutAsync<UsuarioDto>(
                $"/api/v1/usuarios/{id}/contrasena",
                new CambiarContrasenaDto { Contrasena = contrasena },
                cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
