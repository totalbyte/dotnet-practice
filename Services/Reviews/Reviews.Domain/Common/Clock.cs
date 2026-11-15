namespace Reviews.Domain.Common;

internal static class Clock
{
    public static DateTime UtcNow
    {
        get
        {
            var now = DateTime.UtcNow;
            return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));
        }
    }
}
