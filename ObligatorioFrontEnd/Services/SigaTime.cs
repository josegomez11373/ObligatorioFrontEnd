namespace ObligatorioFrontEnd.Services;

/// <summary>
/// Centraliza la zona horaria del sistema. Todo se guarda en UTC y se muestra
/// en la hora de Uruguay (America/Montevideo).
/// </summary>
public static class SigaTime
{
    private static readonly TimeZoneInfo Montevideo = ResolveTimeZone();

    public static DateTime UtcNow => DateTime.UtcNow;

    public static DateTime NowLocal => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Montevideo);

    public static DateTime TodayLocal => NowLocal.Date;

    public static DateTime ToLocal(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, Montevideo);
    }

    public static string Format(DateTime value, string format = "g") => ToLocal(value).ToString(format);

    /// <summary>
    /// Formatea una fecha para tablas. La API todavía devuelve <c>default</c> en
    /// algunas fechas de creación (bug de auditoría), así que se muestra una raya.
    /// </summary>
    public static string FormatOrDash(DateTime value, string format = "g") =>
        value == default || value.Year < 2000 ? "—" : Format(value, format);

    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var id in new[] { "America/Montevideo", "Montevideo Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
