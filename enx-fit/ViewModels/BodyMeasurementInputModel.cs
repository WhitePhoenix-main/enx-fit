using System.ComponentModel.DataAnnotations;
using enx_fit.Models;
using Microsoft.AspNetCore.Mvc;
using enx_fit.ModelBinding;

namespace enx_fit.ViewModels;

public class BodyMeasurementInputModel
{
    [Display(Name = "Пользователь")]
    public string? OwnerId { get; set; }

    [Required(ErrorMessage = "Укажите дату замера.")]
    [Display(Name = "Дата замера")]
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; }

    [ModelBinder(BinderType = typeof(BodyDecimalModelBinder))]
    [Range(typeof(decimal), "1", "500", ErrorMessage = "Укажите вес от 1 до 500 кг.")]
    [Display(Name = "Вес, кг")]
    public decimal WeightKg { get; set; }

    [ModelBinder(BinderType = typeof(BodyDecimalModelBinder))]
    [Range(typeof(decimal), "50", "300", ErrorMessage = "Укажите рост от 50 до 300 см.")]
    [Display(Name = "Рост, см")]
    public decimal HeightCm { get; set; }

    [ModelBinder(BinderType = typeof(BodyDecimalModelBinder))]
    [Range(typeof(decimal), "0", "100", ErrorMessage = "Укажите значение от 0 до 100%.")]
    [Display(Name = "Жир, %")]
    public decimal? BodyFatPercent { get; set; }

    [ModelBinder(BinderType = typeof(BodyDecimalModelBinder))]
    [Range(typeof(decimal), "1", "300", ErrorMessage = "Укажите обхват от 1 до 300 см.")]
    [Display(Name = "Талия, см")]
    public decimal? WaistCm { get; set; }

    [ModelBinder(BinderType = typeof(BodyDecimalModelBinder))]
    [Range(typeof(decimal), "1", "300", ErrorMessage = "Укажите обхват от 1 до 300 см.")]
    [Display(Name = "Грудь, см")]
    public decimal? ChestCm { get; set; }

    [ModelBinder(BinderType = typeof(BodyDecimalModelBinder))]
    [Range(typeof(decimal), "1", "300", ErrorMessage = "Укажите обхват от 1 до 300 см.")]
    [Display(Name = "Ягодицы, см")]
    public decimal? HipCm { get; set; }

    [ModelBinder(BinderType = typeof(BodyDecimalModelBinder))]
    [Range(typeof(decimal), "1", "150", ErrorMessage = "Укажите обхват от 1 до 150 см.")]
    [Display(Name = "Рука, см")]
    public decimal? ArmCm { get; set; }

    [ModelBinder(BinderType = typeof(BodyDecimalModelBinder))]
    [Range(typeof(decimal), "1", "200", ErrorMessage = "Укажите обхват от 1 до 200 см.")]
    [Display(Name = "Бедро, см")]
    public decimal? ThighCm { get; set; }

    [Display(Name = "Заметки")]
    [StringLength(1000, ErrorMessage = "Заметка должна быть не длиннее 1000 символов.")]
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
