using System.ComponentModel.DataAnnotations.Schema;

namespace enx_fit.Models;

public class BodyMeasurement
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public decimal WeightKg { get; set; }

    public decimal HeightCm { get; set; }

    public decimal? BodyFatPercent { get; set; }

    public decimal? WaistCm { get; set; }

    public decimal? ChestCm { get; set; }

    public decimal? HipCm { get; set; }

    public decimal? ArmCm { get; set; }

    public decimal? ThighCm { get; set; }

    public string? Notes { get; set; }

    [NotMapped]
    public decimal Bmi => HeightCm <= 0
        ? 0
        : Math.Round(WeightKg / ((HeightCm / 100) * (HeightCm / 100)), 1);
}
