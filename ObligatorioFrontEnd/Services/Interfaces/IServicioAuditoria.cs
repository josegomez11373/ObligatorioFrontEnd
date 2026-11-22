using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

public interface IServicioAuditoria
{
    Task<AuditoriaPaginadaDto> BuscarAsync(
        string? entidad = null,
        string? actor = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        Guid? empresaId = null,
        int pagina = 1,
        int tamanoPagina = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recorre todas las páginas (200 registros por vez) para alimentar los
    /// reportes sin quedar limitado a la primera página.
    /// </summary>
    Task<IReadOnlyList<AuditoriaDto>> BuscarTodoAsync(
        string? entidad = null,
        string? actor = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        Guid? empresaId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetEntidadesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetActoresAsync(
        CancellationToken cancellationToken = default);
}
