namespace AgendaiFisio.Web.Services;

public static class BrazilTime
{
    private static readonly Lazy<TimeZoneInfo> Zone = new(() =>
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.Utc;
    });

    public static DateTime ToUtc(DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, Zone.Value);
    }

    public static DateTime ToLocal(DateTime utc)
    {
        var normalized = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(normalized, Zone.Value);
    }
}
