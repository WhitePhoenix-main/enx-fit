namespace enx_fit.Models;

public sealed class DailyCheckIn
{
    public string UserId { get; set; } = "";
    public DateOnly Date { get; set; }
    public int WaterMl { get; set; }
    public int Steps { get; set; }
    public int SleepMinutes { get; set; }
    public bool NutritionLogged { get; set; }
    public bool StretchingDone { get; set; }
}
