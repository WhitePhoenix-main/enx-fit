using enx_fit.Models;
using enx_fit.ViewModels;

namespace enx_fit.Services;

public sealed record WorkoutExerciseBlock(string Key, string Name, string Kind)
{
    public string Type => Kind switch { "warmup" => "Разминка", "superset" => "Суперсет", "cooldown" => "Заминка", _ => "Силовой блок" };
}

// Group identity comes from the immutable builder plan. Extra exercises retain their own segment.
public static class WorkoutExerciseBlocks
{
    public static IReadOnlyDictionary<int, WorkoutExerciseBlock> Describe(WorkoutSession session)
    {
        var result = new Dictionary<int, WorkoutExerciseBlock>();
        WorkoutBuilderInput? blueprint = null;
        if (session.BuilderConfigurationJson is { } json) WorkoutBuilderInput.TryParse(json, out blueprint);
        var planned = blueprint?.Blocks.Select((b, i) => new { Block = b, Index = i }).ToArray();
        var segment = -1; string? previous = null;
        foreach (var exercise in session.WorkoutExercises.OrderBy(e => e.Order).ThenBy(e => e.Id))
        {
            var source = planned?.FirstOrDefault(b => b.Block.Kind == exercise.BlockKind && b.Block.Exercises.Any(e => e.ExerciseId == exercise.ExerciseId));
            var ambiguous = false;
            if (source is null && planned is not null)
            {
                var matches = planned.Where(b => b.Block.Kind == exercise.BlockKind && b.Block.Name == exercise.Notes).ToArray();
                if (matches.Length == 1) source = matches[0];
                ambiguous = matches.Length > 1;
            }
            if (source is not null)
                result[exercise.Id] = new("plan:" + source.Index, source.Block.Name, source.Block.Kind);
            else
            {
                var kind = exercise.BlockKind is "warmup" or "superset" or "cooldown" ? exercise.BlockKind : "strength";
                var name = kind == "strength" ? (planned is null ? "Основные упражнения" : "Дополнительные упражнения") : exercise.Notes ?? "Отдельный блок";
                var signature = kind + ":" + (kind == "strength" ? "" : name) + (ambiguous ? ":" + exercise.Id : "");
                if (signature != previous) segment++;
                previous = signature;
                result[exercise.Id] = new("extra:" + segment, name, kind);
            }
            // A planned group separates any free segments before and after it.
            if (source is not null) previous = null;
        }
        return result;
    }
}
