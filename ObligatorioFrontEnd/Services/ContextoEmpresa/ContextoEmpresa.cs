using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Services;

public sealed class ContextoEmpresa : IContextoEmpresa
{
    private readonly IAuthService _auth;
    private readonly IServicioEmpresas _empresas;
    private readonly NotificadorDatos _notificador;
    private readonly ProtectedSessionStorage _storage;

    private readonly List<EmpresaDto> _disponibles = [];

    public ContextoEmpresa(
        IAuthService auth,
        IServicioEmpresas empresas,
        NotificadorDatos notificador,
        ProtectedSessionStorage storage)
    {
        _auth = auth;
        _empresas = empresas;
        _notificador = notificador;
        _storage = storage;

        // Si se crea o desactiva una empresa, el selector del administrador general
        // debe reflejarlo sin exigir recargar la página.
        _notificador.Cambio += HandleNotificacion;
    }

    private void HandleNotificacion(string modulo)
    {
        if (modulo != ModulosDatos.Empresas)
        {
            return;
        }

        _ = RefrescarSeguroAsync();
    }

    private async Task RefrescarSeguroAsync()
    {
        try
        {
            await RefrescarAsync();
        }
        catch
        {
            // El refresco del selector no debe romper la operación que lo originó.
        }
    }

    public Guid? EmpresaActivaId { get; private set; }

    public EmpresaDto? EmpresaActiva =>
        _disponibles.FirstOrDefault(empresa => empresa.Id == EmpresaActivaId);

    public IReadOnlyList<EmpresaDto> Disponibles => _disponibles;

    public bool PuedeCambiar => _auth.CurrentUser?.Role == UserRole.AdministradorGeneral;

    public event Action? Cambiada;

    public async Task InicializarAsync(CancellationToken cancellationToken = default)
    {
        var usuario = _auth.CurrentUser;
        _disponibles.Clear();

        if (usuario is null)
        {
            EmpresaActivaId = null;
            return;
        }

        if (usuario.EmpresaId is Guid fija)
        {
            _disponibles.Add(new EmpresaDto(
                fija,
                usuario.EmpresaNombre ?? "Mi empresa",
                null,
                true,
                default,
                0));
            EmpresaActivaId = fija;
            Cambiada?.Invoke();
            return;
        }

        try
        {
            var empresas = await _empresas.GetAllAsync(includeInactive: false, cancellationToken);
            _disponibles.AddRange(empresas.OrderBy(empresa => empresa.Nombre));
        }
        catch (ApiException)
        {
            // Sin conexión no se puede armar el selector; las páginas mostrarán su error.
        }

        var recordada = await LeerEmpresaRecordadaAsync();
        EmpresaActivaId = recordada is Guid id && _disponibles.Any(empresa => empresa.Id == id)
            ? id
            : _disponibles.FirstOrDefault()?.Id;

        Cambiada?.Invoke();
    }

    public async Task<bool> SeleccionarAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        if (!PuedeCambiar)
        {
            return false;
        }

        var empresa = _disponibles.FirstOrDefault(candidate => candidate.Id == empresaId);
        if (empresa is null || !empresa.Activo || !_auth.CanAccessEmpresa(empresaId))
        {
            return false;
        }

        EmpresaActivaId = empresaId;
        await GuardarEmpresaRecordadaAsync(empresaId);
        Cambiada?.Invoke();
        _notificador.Notificar(ModulosDatos.EmpresaActiva);
        return true;
    }

    public async Task RefrescarAsync(CancellationToken cancellationToken = default)
    {
        var anterior = EmpresaActivaId;
        await InicializarAsync(cancellationToken);

        if (anterior != EmpresaActivaId || anterior is not null)
        {
            _notificador.Notificar(ModulosDatos.EmpresaActiva);
        }
    }

    public void Limpiar()
    {
        _disponibles.Clear();
        EmpresaActivaId = null;
        _ = BorrarEmpresaRecordadaAsync();
        Cambiada?.Invoke();
    }

    private async Task<Guid?> LeerEmpresaRecordadaAsync()
    {
        try
        {
            var resultado = await _storage.GetAsync<Guid>(SesionKeys.EmpresaActiva);
            return resultado.Success ? resultado.Value : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task GuardarEmpresaRecordadaAsync(Guid empresaId)
    {
        try
        {
            await _storage.SetAsync(SesionKeys.EmpresaActiva, empresaId);
        }
        catch
        {
            // Sin JS disponible (por ejemplo en una prueba de integración) se ignora.
        }
    }

    private async Task BorrarEmpresaRecordadaAsync()
    {
        try
        {
            await _storage.DeleteAsync(SesionKeys.EmpresaActiva);
        }
        catch
        {
            // Sin JS disponible se ignora.
        }
    }
}
