using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioEventos
{
    Task<IReadOnlyList<EventoDto>> GetAllAsync(
        bool incluirAnulados = true,
        Guid? campanaId = null,
        Guid? potreroId = null,
        Guid? tipoEventoId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancellationToken = default);

    Task<EventoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<EventoDto> CreateAsync(
        CrearEventoDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventoDto>> CreateForPotrerosAsync(
        CrearEventosLoteDto dto,
        CancellationToken cancellationToken = default);

    Task<EventoDto?> UpdateAsync(
        ActualizarEventoDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> AnularAsync(
        Guid id,
        string motivo,
        CancellationToken cancellationToken = default);
}
