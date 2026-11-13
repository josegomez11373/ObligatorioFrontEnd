using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioTiposTarea
{
    Task<IReadOnlyList<TipoTareaDto>> GetAllAsync(
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default);

    Task<TipoTareaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TipoTareaDto> CreateAsync(
        CrearTipoTareaDto dto,
        CancellationToken cancellationToken = default);

    Task<TipoTareaDto?> UpdateAsync(
        ActualizarTipoTareaDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
