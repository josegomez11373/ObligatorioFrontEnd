using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioUsuarios
{
    Task<IReadOnlyList<UsuarioDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default);

    Task<UsuarioDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UsuarioDto> CreateAsync(
        CrearUsuarioDto dto,
        CancellationToken cancellationToken = default);

    Task<UsuarioDto?> UpdateAsync(
        ActualizarUsuarioDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UsuarioDto?> CambiarContrasenaAsync(
        Guid id,
        string contrasena,
        CancellationToken cancellationToken = default);
}
