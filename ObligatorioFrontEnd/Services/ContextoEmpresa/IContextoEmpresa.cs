using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Empresa sobre la que operan los módulos migrados. El administrador general
/// puede cambiarla; el resto tiene la empresa fija del token.
/// </summary>
public interface IContextoEmpresa
{
    Guid? EmpresaActivaId { get; }

    EmpresaDto? EmpresaActiva { get; }

    IReadOnlyList<EmpresaDto> Disponibles { get; }

    bool PuedeCambiar { get; }

    event Action? Cambiada;

    Task InicializarAsync(CancellationToken cancellationToken = default);

    Task<bool> SeleccionarAsync(Guid empresaId, CancellationToken cancellationToken = default);

    Task RefrescarAsync(CancellationToken cancellationToken = default);

    void Limpiar();
}
