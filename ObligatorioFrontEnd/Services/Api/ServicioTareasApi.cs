using System.Globalization;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;

namespace ObligatorioFrontEnd.Services;

/// <summary>Tareas y su ciclo de vida contra ApiSiga.</summary>
public sealed class ServicioTareasApi : IServicioTareas
{
    private readonly ClienteApi _api;
    private readonly NotificadorDatos _notificador;

    public ServicioTareasApi(ClienteApi api, NotificadorDatos notificador)
    {
        _api = api;
        _notificador = notificador;
    }

    public async Task<IReadOnlyList<TareaDto>> GetAllAsync(
        bool incluirAnuladas = true,
        Guid? campanaId = null,
        Guid? potreroId = null,
        EstadoTarea? estado = null,
        Guid? ejecutorId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancellationToken = default)
    {
        var filtros = new List<string>
        {
            $"incluirAnuladas={incluirAnuladas.ToString().ToLowerInvariant()}"
        };

        if (campanaId is Guid campana)
        {
            filtros.Add($"campanaId={campana}");
        }

        if (potreroId is Guid potrero)
        {
            filtros.Add($"potreroId={potrero}");
        }

        if (estado is EstadoTarea estadoTarea)
        {
            filtros.Add($"estado={estadoTarea}");
        }

        if (ejecutorId is Guid ejecutor)
        {
            filtros.Add($"ejecutorId={ejecutor}");
        }

        if (desde is DateTime fechaDesde)
        {
            filtros.Add($"desde={fechaDesde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }

        if (hasta is DateTime fechaHasta)
        {
            filtros.Add($"hasta={fechaHasta.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }

        return await _api.GetAsync<List<TareaDto>>(
            $"/api/v1/tareas?{string.Join("&", filtros)}", cancellationToken);
    }

    public async Task<TareaDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetAsync<TareaDto>($"/api/v1/tareas/{id}", cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<TareaDto> CreateAsync(
        CrearTareaDto dto,
        CancellationToken cancellationToken = default)
    {
        var creada = await _api.PostAsync<TareaDto>(
            "/api/v1/tareas", dto, cancellationToken);

        _notificador.Notificar(ModulosDatos.Tareas);
        return creada;
    }

    public async Task<TareaDto?> UpdateAsync(
        ActualizarTareaDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualizada = await _api.PutAsync<TareaDto>(
                $"/api/v1/tareas/{dto.Id}", dto, cancellationToken);

            _notificador.Notificar(ModulosDatos.Tareas);
            return actualizada;
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public Task<bool> AssignAsync(
        Guid id,
        Guid ejecutorId,
        TipoEjecutorTarea ejecutorTipo,
        long version,
        DateTime? fechaPlanificada = null,
        CancellationToken cancellationToken = default) =>
        AccionAsync(
            $"/api/v1/tareas/{id}/asignar",
            new AsignarTareaDto
            {
                AsignadaAUsuarioId = ejecutorTipo == TipoEjecutorTarea.Usuario
                    ? ejecutorId
                    : null,
                AsignadaAContratistaId = ejecutorTipo == TipoEjecutorTarea.Contratista
                    ? ejecutorId
                    : null,
                FechaPlanificada = fechaPlanificada,
                Version = version
            },
            cancellationToken);

    public Task<bool> RealizarAsync(
        Guid id,
        RealizarTareaDto dto,
        CancellationToken cancellationToken = default) =>
        AccionAsync($"/api/v1/tareas/{id}/realizar", dto, cancellationToken);

    public Task<bool> VerifyAsync(
        Guid id,
        VerificarTareaDto dto,
        CancellationToken cancellationToken = default) =>
        AccionAsync($"/api/v1/tareas/{id}/verificar", dto, cancellationToken);

    public Task<bool> AnularAsync(
        Guid id,
        AnularTareaDto dto,
        CancellationToken cancellationToken = default) =>
        AccionAsync($"/api/v1/tareas/{id}/anular", dto, cancellationToken);

    private async Task<bool> AccionAsync(
        string ruta,
        object cuerpo,
        CancellationToken cancellationToken)
    {
        try
        {
            await _api.PutAsync<TareaDto>(ruta, cuerpo, cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == 404)
        {
            return false;
        }

        _notificador.Notificar(ModulosDatos.Tareas);
        return true;
    }
}
