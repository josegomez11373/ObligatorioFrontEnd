using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioInsumos
{
    Task<IReadOnlyList<InsumoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default);

    Task<InsumoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InsumoDto> CreateAsync(
        CrearInsumoDto dto,
        CancellationToken cancellationToken = default);

    Task<InsumoDto?> UpdateAsync(
        ActualizarInsumoDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
