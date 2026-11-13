namespace ObligatorioFrontEnd.Api.Contratos;

/// <summary>Registro de auditoría devuelto por la API.</summary>
public sealed record AuditoriaDto(
    Guid Id,
    Guid? EmpresaId,
    Guid? UsuarioId,
    string? Actor,
    string? ActorEmail,
    DateTime FechaHoraUtc,
    string Entidad,
    Guid EntidadId,
    AccionAuditoria Accion,
    string? DatosAnteriores,
    string? DatosNuevos);

/// <summary>Página de auditoría. La API devuelve como máximo 200 registros por página.</summary>
public sealed class AuditoriaPaginadaDto
{
    private readonly IReadOnlyList<AuditoriaDto>? items;

    /// <summary>Se normaliza a vacía si la API envía <c>null</c>.</summary>
    public IReadOnlyList<AuditoriaDto> Items
    {
        get => items ?? [];
        init => items = value;
    }

    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int Total { get; init; }
}
