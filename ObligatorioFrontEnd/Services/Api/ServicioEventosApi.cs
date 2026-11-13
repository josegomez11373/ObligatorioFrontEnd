using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Eventos de campo contra ApiSiga.</summary>
public sealed class ServicioEventosApi : IServicioEventos
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioEventosApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<EventoDto>> GetAllAsync(
        bool incluirAnulados = true,
        Guid? campanaId = null,
        Guid? potreroId = null,
        Guid? tipoEventoId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancellationToken = default)
    {
        var filtros = new List<string>
        {
            $"incluirAnulados={incluirAnulados.ToString().ToLowerInvariant()}"
        };

        if (campanaId is Guid campana)
        {
            filtros.Add($"campanaId={campana}");
        }

        if (potreroId is Guid potrero)
        {
            filtros.Add($"potreroId={potrero}");
        }

        if (tipoEventoId is Guid tipo)
        {
            filtros.Add($"tipoEventoId={tipo}");
        }

        if (desde is DateTime fechaDesde)
        {
            filtros.Add($"desde={fechaDesde:yyyy-MM-dd}");
        }

        if (hasta is DateTime fechaHasta)
        {
            filtros.Add($"hasta={fechaHasta:yyyy-MM-dd}");
        }

        return await _api.GetAsync<List<EventoDto>>(
            $"/api/v1/eventos?{string.Join("&", filtros)}", cancellationToken);
    }

    public async Task<EventoDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<EventoDto>($"/api/v1/eventos/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<EventoDto> CreateAsync(
        CrearEventoDto dto,
        CancellationToken cancellationToken = default)
    {
        var creado = await _api.PostAsync<EventoDto>(
            "/api/v1/eventos", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Eventos);
        return creado;
    }

    public async Task<IReadOnlyList<EventoDto>> CreateForPotrerosAsync(
        CrearEventosLoteDto dto,
        CancellationToken cancellationToken = default)
    {
        // La API no tiene endpoint de lote: se crea un evento por potrero.
        var creados = new List<EventoDto>();

        foreach (var campanaPotreroId in dto.CampanaPotreroIds)
        {
            var creado = await _api.PostAsync<EventoDto>(
                "/api/v1/eventos",
                new CrearEventoDto
                {
                    EmpresaId = dto.EmpresaId,
                    CampanaPotreroId = campanaPotreroId,
                    TipoEventoId = dto.TipoEventoId,
                    Fecha = dto.Fecha,
                    Descripcion = dto.Descripcion,
                    Ubicacion = dto.Ubicacion,
                    Latitud = dto.Latitud,
                    Longitud = dto.Longitud
                },
                cancellationToken);

            creados.Add(creado);
        }

        _notificador.Notificar(ModulosDatos.Eventos);
        return creados;
    }

    public async Task<EventoDto?> UpdateAsync(
        ActualizarEventoDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizado = await _api.PutAsync<EventoDto>(
                $"/api/v1/eventos/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Eventos);
            return actualizado;
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> AnularAsync(
        Guid id,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruta = $"/api/v1/eventos/{id}";
            if (!string.IsNullOrWhiteSpace(motivo))
            {
                ruta += $"?motivoAnulacion={Uri.EscapeDataString(motivo)}";
            }

            await _api.DeleteAsync(ruta, cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Eventos);
        return true;
    }
}
