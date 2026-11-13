using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioMaquinaria
{
    Task<IReadOnlyList<MaquinariaDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default);

    Task<MaquinariaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MaquinariaDto> CreateAsync(
        CrearMaquinariaDto dto,
        CancellationToken cancellationToken = default);

    Task<MaquinariaDto?> UpdateAsync(
        ActualizarMaquinariaDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
