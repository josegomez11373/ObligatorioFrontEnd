using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioContratistas
{
    Task<IReadOnlyList<ContratistaDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default);

    Task<ContratistaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ContratistaDto> CreateAsync(
        CrearContratistaDto dto,
        CancellationToken cancellationToken = default);

    Task<ContratistaDto?> UpdateAsync(
        ActualizarContratistaDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
