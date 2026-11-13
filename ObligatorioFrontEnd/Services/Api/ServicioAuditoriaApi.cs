using System.Globalization;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Consulta de auditoría contra ApiSiga.</summary>
public sealed class ServicioAuditoriaApi : IServicioAuditoria
{
    private const int TamanoPaginaMaximo = 200;

    private readonly ClienteApi _api;

    public ServicioAuditoriaApi(ClienteApi api)
    {
        _api = api;
    }

    public async Task<AuditoriaPaginadaDto> BuscarAsync(
        string? entidad = null,
        string? actor = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        Guid? empresaId = null,
        int pagina = 1,
        int tamanoPagina = 50,
        CancellationToken cancellationToken = default)
    {
        var filtros = new List<string>
        {
            $"pagina={pagina.ToString(CultureInfo.InvariantCulture)}",
            $"tamanoPagina={tamanoPagina.ToString(CultureInfo.InvariantCulture)}"
        };

        if (!string.IsNullOrWhiteSpace(entidad))
        {
            filtros.Add($"entidad={Uri.EscapeDataString(entidad)}");
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            filtros.Add($"actor={Uri.EscapeDataString(actor)}");
        }

        if (desde is DateTime fechaDesde)
        {
            filtros.Add($"desde={fechaDesde:yyyy-MM-dd}");
        }

        if (hasta is DateTime fechaHasta)
        {
            filtros.Add($"hasta={fechaHasta:yyyy-MM-dd}");
        }

        if (empresaId is Guid empresa)
        {
            filtros.Add($"empresaId={empresa}");
        }

        return await _api.GetAsync<AuditoriaPaginadaDto>(
            $"/api/v1/auditoria?{string.Join("&", filtros)}", cancellationToken);
    }

    public async Task<IReadOnlyList<AuditoriaDto>> BuscarTodoAsync(
        string? entidad = null,
        string? actor = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        Guid? empresaId = null,
        CancellationToken cancellationToken = default)
    {
        var registros = new List<AuditoriaDto>();
        var pagina = 1;
        var total = 0;

        do
        {
            var respuesta = await BuscarAsync(
                entidad,
                actor,
                desde,
                hasta,
                empresaId,
                pagina,
                TamanoPaginaMaximo,
                cancellationToken);

            registros.AddRange(respuesta.Items);
            total = respuesta.Total;

            if (respuesta.Items.Count == 0)
            {
                break;
            }

            pagina++;
        }
        while (registros.Count < total);

        return registros;
    }

    public async Task<IReadOnlyList<string>> GetEntidadesAsync(
        CancellationToken cancellationToken = default)
    {
        var registros = await BuscarTodoAsync(cancellationToken: cancellationToken);
        return registros
            .Select(registro => registro.Entidad)
            .Where(entidad => !string.IsNullOrWhiteSpace(entidad))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(entidad => entidad)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetActoresAsync(
        CancellationToken cancellationToken = default)
    {
        var registros = await BuscarTodoAsync(cancellationToken: cancellationToken);
        return registros
            .Select(registro => registro.Actor)
            .Where(actor => !string.IsNullOrWhiteSpace(actor))
            .Select(actor => actor!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(actor => actor)
            .ToList();
    }
}
