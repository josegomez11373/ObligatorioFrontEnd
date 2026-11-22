using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioCultivos
{
    Task<IReadOnlyList<CultivoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default);

    Task<CultivoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CultivoDto> CreateAsync(
        CrearCultivoDto dto,
        CancellationToken cancellationToken = default);

    Task<CultivoDto?> UpdateAsync(
        ActualizarCultivoDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
