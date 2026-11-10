using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioCampanas
{
    Task<IReadOnlyList<CampanaDto>> GetAllAsync(
        bool incluirAnuladas = false,
        CancellationToken cancellationToken = default);

    Task<CampanaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CampanaDto> CreateAsync(
        CrearCampanaDto dto,
        CancellationToken cancellationToken = default);

    Task<CampanaDto?> UpdateAsync(
        ActualizarCampanaDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> AnularAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
