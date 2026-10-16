namespace enx_fit.Models;

public enum SetupStatus { NotStarted, Completed, Skipped }
public enum TrainingGoal { Regularity, Strength, Muscle, Weight, GeneralFitness }
public enum TrainingLocation { Home, Gym, Outdoors, Varies }
[Flags]
public enum TrainingEquipment { None = 0, Dumbbells = 1, Barbell = 2, Bench = 4, PullupBar = 8, LegPress = 16, Bands = 32, Kettlebell = 64 }

public static class TrainingPreferences
{
    public static readonly (TrainingGoal Value, string Title, string Description)[] Goals = [
        (TrainingGoal.Regularity, "Тренироваться регулярно", "Записывать занятия и видеть свой ритм."),
        (TrainingGoal.Strength, "Развивать силу", "Следить за рабочими весами и повторениями."),
        (TrainingGoal.Muscle, "Набрать мышечную массу", "Планировать занятия и отслеживать результаты."),
        (TrainingGoal.Weight, "Изменить вес", "Сопоставлять замеры и историю занятий."),
        (TrainingGoal.GeneralFitness, "Общая подготовка", "Двигаться и постепенно осваивать упражнения.")
    ];
    public static readonly (TrainingLocation Value, string Title)[] Locations = [
        (TrainingLocation.Home, "Дома"), (TrainingLocation.Gym, "В зале"), (TrainingLocation.Outdoors, "На улице"), (TrainingLocation.Varies, "В разных местах")
    ];
    public static readonly (TrainingEquipment Value, string Title)[] Equipment = [
        (TrainingEquipment.Dumbbells, "Гантели"), (TrainingEquipment.Barbell, "Штанга"), (TrainingEquipment.Bench, "Скамья"),
        (TrainingEquipment.PullupBar, "Турник"), (TrainingEquipment.LegPress, "Тренажёр для жима ногами"),
        (TrainingEquipment.Bands, "Эспандер / резинки"), (TrainingEquipment.Kettlebell, "Гири")
    ];
    public static readonly string[] Days = ["Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"];
    public static string GoalTitle(TrainingGoal? goal) => Goals.FirstOrDefault(g => g.Value == goal).Title ?? "Цель пока не выбрана";
    public static string LocationTitle(TrainingLocation? location) => Locations.FirstOrDefault(l => l.Value == location).Title ?? "Место пока не выбрано";
    public static int[] SelectedDays(int mask) => Enumerable.Range(1, 7).Where(day => (mask & (1 << (day - 1))) != 0).ToArray();
    public static string DaysTitle(int mask) => mask == 0 ? "Дни выберу позже" : string.Join(", ", SelectedDays(mask).Select(d => Days[d - 1]));
    public static string EquipmentTitle(TrainingEquipment equipment) => equipment == TrainingEquipment.None ? "Без дополнительного оборудования" : string.Join(", ", Equipment.Where(e => equipment.HasFlag(e.Value)).Select(e => e.Title));

    // Unknown requirements stay incompatible. A location never implies access to equipment.
    public static bool HasEquipment(TrainingProgram program, TrainingEquipment available) => program.Workouts.Count > 0 && program.Workouts.All(w => w.Exercises.Count > 0 && w.Exercises.All(e =>
        CoversAll(e.Exercise.Equipment, available)));
    private static bool CoversAll(string? requirements, TrainingEquipment available)
    {
        var tokens = requirements?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];
        return tokens.Length > 0 && tokens.All(token => Covers(token, available));
    }
    private static bool Covers(string token, TrainingEquipment available)
    {
        var equipment = token.ToLowerInvariant() switch {
            "без оборудования" => TrainingEquipment.None,
            "гантели" or "гантель" => TrainingEquipment.Dumbbells,
            "штанга" => TrainingEquipment.Barbell,
            "скамья" => TrainingEquipment.Bench,
            "турник" => TrainingEquipment.PullupBar,
            "тренажёр для жима ногами" => TrainingEquipment.LegPress,
            "эспандер" or "резиновая петля" => TrainingEquipment.Bands,
            "гиря" or "гири" => TrainingEquipment.Kettlebell,
            _ => (TrainingEquipment)(-1)
        };
        return equipment != (TrainingEquipment)(-1) && (equipment == TrainingEquipment.None || available.HasFlag(equipment));
    }
}
