using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioTareas
{
    Task<IReadOnlyList<TareaDto>> GetAllAsync(
        bool incluirAnuladas = true,
        Guid? campanaId = null,
        Guid? potreroId = null,
        EstadoTarea? estado = null,
        Guid? ejecutorId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancellationToken = default);

    Task<TareaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TareaDto> CreateAsync(
        CrearTareaDto dto,
        CancellationToken cancellationToken = default);

    Task<TareaDto?> UpdateAsync(
        ActualizarTareaDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> AssignAsync(
        Guid id,
        Guid ejecutorId,
        TipoEjecutorTarea ejecutorTipo,
        long version,
        DateTime? fechaPlanificada = null,
        CancellationToken cancellationToken = default);

    Task<bool> RealizarAsync(
        Guid id,
        RealizarTareaDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(
        Guid id,
        VerificarTareaDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> AnularAsync(
        Guid id,
        AnularTareaDto dto,
        CancellationToken cancellationToken = default);
}
