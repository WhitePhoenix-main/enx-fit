using System.ComponentModel.DataAnnotations;
using enx_fit.Models;

namespace enx_fit.ViewModels;

public class BodyMeasurementInputModel
{
    [Display(Name = "Пользователь")]
    public string? OwnerId { get; set; }

    [Required]
    [Display(Name = "Дата замера")]
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(typeof(decimal), "1", "500")]
    [Display(Name = "Вес, кг")]
    public decimal WeightKg { get; set; }

    [Range(typeof(decimal), "50", "300")]
    [Display(Name = "Рост, см")]
    public decimal HeightCm { get; set; }

    [Range(typeof(decimal), "0", "100")]
    [Display(Name = "Жир, %")]
    public decimal? BodyFatPercent { get; set; }

    [Range(typeof(decimal), "1", "300")]
    [Display(Name = "Талия, см")]
    public decimal? WaistCm { get; set; }

    [Range(typeof(decimal), "1", "300")]
    [Display(Name = "Грудь, см")]
    public decimal? ChestCm { get; set; }

    [Range(typeof(decimal), "1", "300")]
    [Display(Name = "Ягодицы, см")]
    public decimal? HipCm { get; set; }

    [Range(typeof(decimal), "1", "150")]
    [Display(Name = "Рука, см")]
    public decimal? ArmCm { get; set; }

    [Range(typeof(decimal), "1", "200")]
    [Display(Name = "Бедро, см")]
    public decimal? ThighCm { get; set; }

    [Display(Name = "Заметки")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    public static BodyMeasurementInputModel FromMeasurement(BodyMeasurement measurement) => new()
    {
        OwnerId = measurement.UserId,
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
