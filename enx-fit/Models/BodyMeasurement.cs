namespace enx_fit.Models;

public class BodyMeasurement
{
    public int Id { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public decimal? WeightKg { get; set; }

    public decimal? BodyFatPercentage { get; set; }

    public decimal? WaistCm { get; set; }

    public decimal? ChestCm { get; set; }

    public decimal? HipCm { get; set; }

    public string? Notes { get; set; }
}
