using enx_fit.Models;

namespace enx_fit.Presentation.Programs;

public static class ProgramTemplatePresentation
{
    public static bool IsStarter(TrainingProgram program) => program.IsTemplate && program.Id is -6 or -7;
    public static string StarterEquipment(TrainingProgram program) => program.Id == -6 ? "Без оборудования" : "Пара гантелей";

    public static string EquipmentSummary(TrainingProgram program)
    {
        if (IsStarter(program)) return StarterEquipment(program);
        var equipment = program.Workouts.SelectMany(w => w.Exercises).Where(e => e.Exercise is not null)
            .SelectMany(e => enx_fit.Presentation.Exercises.ExercisePresentation.EquipmentDetails(e.Exercise))
            .Select(e => e.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return equipment.Count switch { 0 => "Оборудование — в составе плана", <= 2 => string.Join(" · ", equipment), _ => "Несколько видов оборудования" };
    }
}
