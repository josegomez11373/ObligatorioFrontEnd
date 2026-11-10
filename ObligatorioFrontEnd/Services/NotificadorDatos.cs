namespace ObligatorioFrontEnd.Services;

/// <summary>Nombres de módulo usados por <see cref="NotificadorDatos"/>.</summary>
public static class ModulosDatos
{
    public const string Empresas = "empresas";
    public const string Establecimientos = "establecimientos";
    public const string Potreros = "potreros";
    public const string Cultivos = "cultivos";
    public const string Campanas = "campanas";
    public const string Usuarios = "usuarios";
    public const string TiposEvento = "tipos-evento";
    public const string TiposTarea = "tipos-tarea";
    public const string Eventos = "eventos";
    public const string Fotos = "fotos";
    public const string Tareas = "tareas";
    public const string Insumos = "insumos";
    public const string Maquinaria = "maquinaria";
    public const string Contratistas = "contratistas";
    public const string Auditoria = "auditoria";
    public const string EmpresaActiva = "empresa-activa";
}

/// <summary>
/// Reemplaza el <c>StateChanged</c> de cada servicio por un único punto de
/// notificación. Es scoped, así que comparte el circuito de Blazor.
/// </summary>
public sealed class NotificadorDatos
{
    public event Action<string>? Cambio;

    public void Notificar(string modulo) => Cambio?.Invoke(modulo);
}
