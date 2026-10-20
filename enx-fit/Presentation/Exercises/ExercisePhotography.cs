using enx_fit.Models;

namespace enx_fit.Presentation.Exercises;

// These photographs illustrate the actual movement; unknown exercises keep their muscle icon.
public static class ExercisePhotography
{
    // The quick-session atlas adds a seated shoulder press without changing legacy photo maps.
    public static string? QuickKey(Exercise exercise) => Key(exercise) ??
        (ExercisePresentation.DisplayName(exercise).ToLowerInvariant() is var name &&
         name.Contains("жим") && name.Contains("гантел") && name.Contains("сидя") ? "shoulder" : null);

    public static string? Key(Exercise exercise)
    {
        var name = ExercisePresentation.DisplayName(exercise).ToLowerInvariant().Replace('ё', 'е');
        if (name == "жим лежа" || name.Contains("жим") && name.Contains("лежа") && name.Contains("штанг")) return "bench";
        if (name.Contains("верхнего блока")) return "pulldown";
        if (name.Contains("гантел") && (name.Contains("в стороны") || name.Contains("через стороны"))) return "lateral";
        if (name.Contains("присед") && name.Contains("штанг")) return "squat";
        if (name.Contains("становая тяга")) return "deadlift";
        if (name.Contains("жим") && name.Contains("гантел") && (name.Contains("наклон") || name.Contains("лежа"))) return "incline";
        if (name.Contains("тяга гантел") && name.Contains("наклон")) return "row";
        if (name.Contains("армейский жим") || name.Contains("жим штанги стоя") || name == "жим над головой") return "overhead";
        if (name.Contains("подтягиван")) return "pullup";
        if (name.Contains("отжиман") && name.Contains("брусь")) return "dip";
        if (name.Contains("гантел") && (name.Contains("бицепс") || name.Contains("сгибание рук"))) return "curl";
        return null;
    }
}
