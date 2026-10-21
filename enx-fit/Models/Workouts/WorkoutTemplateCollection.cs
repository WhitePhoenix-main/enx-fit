namespace enx_fit.Models;

public sealed class WorkoutTemplateCollection
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public int[] WorkoutIds { get; set; } = [];
}
