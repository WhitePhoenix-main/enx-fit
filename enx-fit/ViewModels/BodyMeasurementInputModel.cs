using System.ComponentModel.DataAnnotations;
using enx_fit.Models;

namespace enx_fit.ViewModels;

public class BodyMeasurementInputModel
{
    [Required]
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(typeof(decimal), "1", "500")]
    [Display(Name = "Weight (kg)")]
    public decimal WeightKg { get; set; }

    [Range(typeof(decimal), "50", "300")]
    [Display(Name = "Height (cm)")]
    public decimal HeightCm { get; set; }

    [Range(typeof(decimal), "0", "100")]
    [Display(Name = "Body fat (%)")]
    public decimal? BodyFatPercent { get; set; }

    [Range(typeof(decimal), "1", "300")]
    [Display(Name = "Waist (cm)")]
    public decimal? WaistCm { get; set; }

    [Range(typeof(decimal), "1", "300")]
    [Display(Name = "Chest (cm)")]
    public decimal? ChestCm { get; set; }

    [Range(typeof(decimal), "1", "300")]
    [Display(Name = "Hip (cm)")]
    public decimal? HipCm { get; set; }

    [Range(typeof(decimal), "1", "150")]
    [Display(Name = "Arm (cm)")]
    public decimal? ArmCm { get; set; }

    [Range(typeof(decimal), "1", "200")]
    [Display(Name = "Thigh (cm)")]
    public decimal? ThighCm { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public static BodyMeasurementInputModel FromMeasurement(BodyMeasurement measurement) => new()
    {
        Date = measurement.Date,
        WeightKg = measurement.WeightKg,
        HeightCm = measurement.HeightCm,
        BodyFatPercent = measurement.BodyFatPercent,
        WaistCm = measurement.WaistCm,
        ChestCm = measurement.ChestCm,
        HipCm = measurement.HipCm,
        ArmCm = measurement.ArmCm,
        ThighCm = measurement.ThighCm,
        Notes = measurement.Notes
    };
}
