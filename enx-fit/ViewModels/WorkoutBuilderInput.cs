using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace enx_fit.ViewModels;

// A prescription, not a completed workout. Actual results remain in SetEntries.
public sealed class WorkoutBuilderInput
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    [Required, StringLength(60)] public string Goal { get; set; } = "Набор массы";
    [Required, RegularExpression("beginner|intermediate|advanced")] public string Level { get; set; } = "intermediate";
    [Range(5, 360)] public int DurationMinutes { get; set; } = 60;
    [Required, RegularExpression("athlete|summit")] public string Cover { get; set; } = "athlete";
    public bool AutoDuration { get; set; } = true;
    public bool ShowRecords { get; set; } = true;
    [Required, MinLength(1), MaxLength(12)] public List<WorkoutBuilderBlock> Blocks { get; set; } = [];

    public static bool TryParse(string json, out WorkoutBuilderInput? input)
    {
        input = null;
        try { input = JsonSerializer.Deserialize<WorkoutBuilderInput>(json, JsonOptions); }
        catch (JsonException) { return false; }
        return input is not null && Valid(input) && input.Blocks is not null && input.Blocks.All(b =>
            b is not null && Valid(b) && b.Exercises is not null && b.Exercises.All(e =>
                e is not null && Valid(e) && e.Sets is not null && e.Sets.All(s => s is not null && Valid(s)))) &&
            input.Blocks.SelectMany(b => b.Exercises).Count() <= 40 &&
            input.Blocks.SelectMany(b => b.Exercises).Select(e => e.ExerciseId).Distinct().Count() == input.Blocks.Sum(b => b.Exercises.Count);
    }

    private static bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), null, true);
}

public sealed class WorkoutBuilderBlock
{
    [Required, StringLength(60)] public string Name { get; set; } = "Силовой блок";
    [Required, RegularExpression("warmup|strength|superset|cooldown")] public string Kind { get; set; } = "strength";
    [Required, MaxLength(40)] public List<WorkoutBuilderExercise> Exercises { get; set; } = [];
}

public sealed class WorkoutBuilderExercise
{
    [Range(1, int.MaxValue)] public int ExerciseId { get; set; }
    [Required, MinLength(1), MaxLength(20)] public List<WorkoutBuilderSet> Sets { get; set; } = [];
}

public sealed class WorkoutBuilderSet
{
    [Range(typeof(decimal), "0", "2000")] public decimal Weight { get; set; }
    [Range(1, 1000)] public int Reps { get; set; } = 12;
    [Range(0, 900)] public int RestSeconds { get; set; } = 90;
}
