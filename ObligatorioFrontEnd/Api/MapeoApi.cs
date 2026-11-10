using ObligatorioFrontEnd.Api.Contratos;
using ObligatorioFrontEnd.Models;

namespace ObligatorioFrontEnd.Api;

/// <summary>
/// Traduce entre los DTOs de la API y los modelos del mock. Es el único
/// lugar donde conviven los dos vocabularios (FechaCreacion ↔ CreatedAt, etc.).
/// </summary>
public static class MapeoApi
{
    public static EmpresaDto AContrato(this Empresa empresa) => new(
        empresa.Id,
        empresa.Nombre,
        null,
        empresa.Activo,
        empresa.CreatedAt,
        empresa.Version);

    public static Empresa AModelo(this EmpresaDto dto) => new()
    {
        Id = dto.Id,
        Nombre = dto.Nombre,
        Activo = dto.Activo,
        CreatedAt = dto.FechaCreacion,
        Version = dto.Version
    };

    public static EstablecimientoDto AContrato(this Establecimiento establecimiento, string empresaNombre) => new(
        establecimiento.Id,
        establecimiento.EmpresaId,
        empresaNombre,
        establecimiento.Nombre,
        establecimiento.Ubicacion,
        establecimiento.Responsables,
        establecimiento.SuperficieHa,
        establecimiento.Activo,
        establecimiento.CreatedAt,
        establecimiento.Version);

    public static PotreroDto AContrato(this Potrero potrero, Establecimiento establecimiento) => new(
        potrero.Id,
        establecimiento.EmpresaId,
        potrero.EstablecimientoId,
        establecimiento.Nombre,
        potrero.Nombre,
        potrero.SuperficieHa,
        potrero.Limites,
        potrero.Activo,
        potrero.CreatedAt,
        potrero.Version);

    public static CultivoDto AContrato(this Cultivo cultivo) => new(
        cultivo.Id,
        cultivo.EmpresaId,
        cultivo.Nombre,
        cultivo.Activo,
        default,
        1);

    public static CampanaPotreroDto AContrato(this CampanaPotrero asignacion, string potreroNombre) => new(
        asignacion.Id,
        asignacion.PotreroId,
        potreroNombre,
        asignacion.FechaInicio,
        asignacion.FechaFin,
        asignacion.Estado.AApi(),
        asignacion.Version);

    public static CampanaDto AContrato(
        this Campana campana,
        string cultivoNombre,
        IEnumerable<CampanaPotreroDto> potreros) => new(
        campana.Id,
        campana.EmpresaId,
        campana.Nombre,
        campana.CultivoId,
        cultivoNombre,
        campana.FechaInicio,
        campana.FechaFin,
        campana.Estado.AApi(),
        campana.CreatedAt,
        campana.Version,
        potreros.ToList());

    public static EstablecimientoAsignadoDto AContrato(this Establecimiento establecimiento) =>
        new(establecimiento.Id, establecimiento.Nombre);

    public static UsuarioDto AContrato(
        this Usuario usuario,
        string? empresaNombre,
        IEnumerable<EstablecimientoAsignadoDto> establecimientos) => new(
        usuario.Id,
        usuario.Nombre,
        usuario.Email,
        usuario.Rol.AApi(),
        usuario.Activo,
        usuario.EmpresaId,
        empresaNombre,
        establecimientos.ToList(),
        usuario.CreatedAt,
        usuario.Version);

    public static RolApi AApi(this UserRole rol) => rol switch
    {
        UserRole.AdministradorGeneral => RolApi.AdministradorGeneral,
        UserRole.AdministradorEmpresa => RolApi.AdministradorEmpresa,
        UserRole.IngenieroAgronomo => RolApi.IngenieroAgronomo,
        UserRole.OperarioCampo => RolApi.OperarioCampo,
        UserRole.Contratista => RolApi.Contratista,
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, "Rol desconocido.")
    };

    public static UserRole AModelo(this RolApi rol) => rol switch
    {
        RolApi.AdministradorGeneral => UserRole.AdministradorGeneral,
        RolApi.AdministradorEmpresa => UserRole.AdministradorEmpresa,
        RolApi.IngenieroAgronomo => UserRole.IngenieroAgronomo,
        RolApi.OperarioCampo => UserRole.OperarioCampo,
        RolApi.Contratista => UserRole.Contratista,
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, "Rol desconocido.")
    };

    public static EstadoCampana AApi(this CampanaEstado estado) => estado switch
    {
        CampanaEstado.Planificada => EstadoCampana.Planificada,
        CampanaEstado.Activa => EstadoCampana.Activa,
        CampanaEstado.Cerrada => EstadoCampana.Cerrada,
        CampanaEstado.Anulada => EstadoCampana.Anulada,
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado desconocido.")
    };

    public static CampanaEstado AModelo(this EstadoCampana estado) => estado switch
    {
        EstadoCampana.Planificada => CampanaEstado.Planificada,
        EstadoCampana.Activa => CampanaEstado.Activa,
        EstadoCampana.Cerrada => CampanaEstado.Cerrada,
        EstadoCampana.Anulada => CampanaEstado.Anulada,
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado desconocido.")
    };
}
