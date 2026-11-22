using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioPotreros
{
    Task<IReadOnlyList<PotreroDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default);

    Task<PotreroDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PotreroDto> CreateAsync(
        CrearPotreroDto dto,
        CancellationToken cancellationToken = default);

    Task<PotreroDto?> UpdateAsync(
        ActualizarPotreroDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
