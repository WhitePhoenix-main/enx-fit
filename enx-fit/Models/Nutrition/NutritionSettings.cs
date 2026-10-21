namespace enx_fit.Models;

public sealed class NutritionSettings
{
    public string UserId { get; set; } = "";
    public int Calories { get; set; } = 2200;
    public int Protein { get; set; } = 120;
    public int Fat { get; set; } = 70;
    public int Carbs { get; set; } = 250;
    public int WaterMl { get; set; } = 2500;
    public string Goal { get; set; } = "Сбалансированное питание";
}
