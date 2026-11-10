using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioEmpresas
{
    Task<IReadOnlyList<EmpresaDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<EmpresaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<EmpresaDto> CreateAsync(
        string nombre,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid id,
        string nombre,
        bool activo,
        long expectedVersion = 0,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
