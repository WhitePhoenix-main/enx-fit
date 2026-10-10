using enx_fit.Models;

namespace enx_fit.Extensions;

public sealed record ExerciseTechniqueGuide(string Name, string Equipment, IReadOnlyList<string> Steps, string Cue,
    string SourceName, string SourceUrl);

// Authored, concise guidance for the starter set. Sources and editorial scope are documented
// in docs/design/2026-10-06/starter-content. Never match custom exercises by name alone.
public static class ExerciseTechniqueCatalog
{
    private static readonly IReadOnlyDictionary<Guid, ExerciseTechniqueGuide> Guides = new Dictionary<Guid, ExerciseTechniqueGuide>
    {
        [ExerciseIds.FromLegacy(-1606)] = new("Приседания с собственным весом", "Без оборудования",
            ["Поставьте стопы чуть шире таза, носки слегка наружу. Напрягите мышцы живота.",
             "Отведите таз назад и плавно согните ноги. Колени направляйте по линии носков, пятки оставляйте на полу.",
             "Опуститесь до глубины, на которой сохраняете положение спины и стоп. Плавно встаньте, выдыхая."],
            "Не торопитесь и не сводите колени внутрь.", "ACE · Bodyweight Squat",
            "https://www.acefitness.org/resources/everyone/exercise-library/135/bodyweight-squat/"),
        [ExerciseIds.FromLegacy(-1026)] = new("Отжимания с колен", "Без оборудования",
            ["Упритесь ладонями и коленями в пол. Ладони чуть шире плеч, взгляд в пол.",
             "Напрягите живот и плавно согните локти, опуская грудь к полу.",
             "Вернитесь вверх. Держите спину ровно, без провисания или сильного прогиба."],
            "Уменьшите число повторений, если ровное положение корпуса теряется.", "Mayo Clinic · Modified pushup",
            "https://www.mayoclinic.org/healthy-lifestyle/fitness/multimedia/modified-pushup/vid-20084674"),
        [ExerciseIds.FromLegacy(-1802)] = new("Ягодичный мост на полу", "Без оборудования",
            ["Лягте на спину, согните колени, поставьте стопы на пол на ширине таза.",
             "Напрягите живот и ягодицы. На выдохе плавно поднимите таз, сохраняя опору на пятки.",
             "Не поднимайте таз за счёт прогиба в пояснице. На вдохе медленно опуститесь."],
            "Выше не значит лучше: сохраняйте контроль поясницы.", "ACE · Glute Bridge",
            "https://www.acefitness.org/resources/everyone/exercise-library/49/glute-bridge/"),
        [ExerciseIds.FromLegacy(-1604)] = new("Приседания с гантелями", "Гантели",
            ["Возьмите по гантели в каждую руку, руки вдоль тела. Стопы примерно на ширине плеч.",
             "Напрягите живот, отведите таз назад и плавно согните ноги. Спина сохраняет естественное положение, колени направлены по линии стоп.",
             "Опуститесь до комфортной глубины, примерно до параллели бёдер с полом, затем плавно встаньте."],
            "Сначала освойте движение с лёгкими гантелями, без рывков.", "Mayo Clinic · Squat with dumbbell",
            "https://www.mayoclinic.org/healthy-lifestyle/fitness/multimedia/squat/vid-20084682"),
        [ExerciseIds.FromLegacy(-1114)] = new("Тяга двух гантелей в наклоне", "Гантели",
            ["Возьмите две лёгкие гантели. Слегка согните колени и наклонитесь от тазобедренных суставов, сохраняя ровную спину.",
             "Руки свободно опущены. Напрягите живот и плавно подтяните гантели, направляя локти назад.",
             "Медленно опустите гантели. Корпус остаётся неподвижным, плечи не заворачиваются вперёд."],
            "Если приходится раскачиваться, уменьшите вес.", "ACE · Kick Start Workout, тяга с гантелями",
            "https://www.acefitness.org/resources/everyone/blog/6595/ace-s-kick-start-workout-phase-ii/")
    };

    public static ExerciseTechniqueGuide? Find(Exercise? exercise) => exercise is not null &&
        Guides.TryGetValue(exercise.Id, out var guide) &&
        string.Equals(exercise.Name.Trim(), guide.Name, StringComparison.Ordinal) &&
        string.Equals(exercise.Equipment.Trim(), guide.Equipment, StringComparison.Ordinal)
        ? guide : null;
}
