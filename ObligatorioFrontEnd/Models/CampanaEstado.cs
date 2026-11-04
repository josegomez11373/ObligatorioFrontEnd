namespace ObligatorioFrontEnd.Models;

public enum CampanaEstado
{
    Planificada,
    Activa,
    Cerrada,
    Anulada
}

public static class CampanaEstadoExtensions
{
    public static string DisplayName(this CampanaEstado estado) => estado switch
    {
        CampanaEstado.Planificada => "Planificada",
        CampanaEstado.Activa => "Activa",
        CampanaEstado.Cerrada => "Cerrada",
        CampanaEstado.Anulada => "Anulada",
        _ => estado.ToString()
    };
}
