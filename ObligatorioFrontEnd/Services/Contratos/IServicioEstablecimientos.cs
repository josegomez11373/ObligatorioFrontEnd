using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioEstablecimientos
{
    Task<IReadOnlyList<EstablecimientoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default);

    Task<EstablecimientoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<EstablecimientoDto> CreateAsync(
        CrearEstablecimientoDto dto,
        CancellationToken cancellationToken = default);

    Task<EstablecimientoDto?> UpdateAsync(
        ActualizarEstablecimientoDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
