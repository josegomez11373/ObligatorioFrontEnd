using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioTiposEvento
{
    Task<IReadOnlyList<TipoEventoDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default);

    Task<TipoEventoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TipoEventoDto> CreateAsync(
        CrearTipoEventoDto dto,
        CancellationToken cancellationToken = default);

    Task<TipoEventoDto?> UpdateAsync(
        ActualizarTipoEventoDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
