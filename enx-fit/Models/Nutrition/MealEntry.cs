namespace enx_fit.Models;

public sealed class MealEntry
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string Type { get; set; } = "Завтрак";
    public string Name { get; set; } = "";
    public int Calories { get; set; }
    public int Protein { get; set; }
    public int Fat { get; set; }
    public int Carbs { get; set; }
}
