using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public interface IUserScope
{
    DemoUser? CurrentUser { get; }
    string ScopeLabel { get; }
    bool CanAccessEmpresa(Guid empresaId);
    bool CanAccessEstablecimiento(Guid establecimientoId);
}
