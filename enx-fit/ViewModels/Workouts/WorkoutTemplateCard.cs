using enx_fit.Models;
using enx_fit.Presentation.Exercises;

namespace enx_fit.ViewModels.Workouts;

public sealed record WorkoutTemplateDetails(WorkoutTemplateCard Card, IReadOnlyList<WorkoutTemplateCard> Similar);

public sealed record WorkoutTemplateCard(TrainingProgram Program, ProgramWorkout Workout, bool Mine,
    int Uses, IReadOnlyList<string> Reasons)
{
    public string Title => Workout.Name;
    public string Level => Program.Level switch { ProgramLevel.Beginner => "Начальный", ProgramLevel.Advanced => "Продвинутый", _ => "Средний" };
    public string Goal => Program.Goal;
    public IReadOnlyList<ProgramWorkoutExercise> Exercises => Workout.Exercises.OrderBy(e => e.Order).ToArray();
    public IReadOnlyList<EquipmentDescription> Equipment => Exercises.SelectMany(e => ExercisePresentation.EquipmentDetails(e.Exercise))
        .DistinctBy(e => e.Name).ToArray();
    public bool Home => Program.Categories.Split('|').Contains("Дом") || Exercises.Count > 0 && Exercises.All(e => e.Exercise.Equipment.Equals("Без оборудования", StringComparison.OrdinalIgnoreCase));
    public string Focus => Exercises.Any(e => IsLegs(e.Exercise.MuscleGroup))
        ? Exercises.Any(e => !IsLegs(e.Exercise.MuscleGroup) && e.Exercise.MuscleGroup is not ("Пресс" or "Мышцы кора")) ? "full" : "legs" : "upper";
    public string Photo => Home ? Focus == "legs" ? "lunge" : "bodyweight"
        : Focus == "legs" || Exercises.FirstOrDefault()?.Exercise.Name.Contains("присед", StringComparison.OrdinalIgnoreCase) == true ? "legs"
        : Program.Goal.Contains("вес", StringComparison.OrdinalIgnoreCase) ? "tone" : Focus == "full" ? "full" : "upper";
    public string Search => string.Join(' ', new[] { Title, Program.Name, Goal, Program.Description, Level, Program.Categories,
        string.Join(' ', Exercises.Select(e => ExercisePresentation.DisplayName(e.Exercise)+" "+e.Exercise.MuscleGroup)), string.Join(' ', Equipment.Select(e => e.Name)),
        Focus=="legs"?"Ноги Низ тела":Focus=="upper"?"Верх тела":"Всё тело Full Body", Home?"Дом Дома":"Зал" });
    private static bool IsLegs(string group) => new[] { "Ноги", "Квадрицепсы", "бедра", "Ягодицы", "Икры" }.Any(x => group.Contains(x, StringComparison.OrdinalIgnoreCase));
}
