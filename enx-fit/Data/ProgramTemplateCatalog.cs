using enx_fit.Models;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Data;

// Small, versioned product catalog, seeded by migrations like the existing Exercise catalog.
internal static class ProgramTemplateCatalog
{
    public static void Seed(ModelBuilder model)
    {
        var created = new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc);
        model.Entity<TrainingProgram>().HasData(
            Template(-1, "Full Body · Первые шаги", "Общая физическая подготовка", "Три занятия на всё тело. Освойте основные движения и подберите комфортную нагрузку.", ProgramLevel.Beginner, 6, 3, "Для начинающих|Full Body"),
            Template(-2, "Upper / Lower", "Набор мышечной массы", "Четыре занятия с чередованием верха и низа тела. Базовая структура для регулярных тренировок.", ProgramLevel.Intermediate, 8, 4, "Набор массы|Upper / Lower"),
            Template(-3, "Силовая база", "Развитие силы", "Три занятия в неделю с акцентом на основные силовые движения и восстановление.", ProgramLevel.Intermediate, 8, 3, "Сила|Full Body"),
            Template(-4, "Движение и тонус", "Снижение веса", "Умеренная силовая нагрузка трижды в неделю. Рабочие веса выбираются индивидуально.", ProgramLevel.Beginner, 6, 3, "Снижение веса|Для начинающих|Full Body"),
            Template(-5, "Push / Pull / Legs", "Набор мышечной массы", "Разделение по движениям с блоками объёма, нагрузки и облегчённой неделей.", ProgramLevel.Advanced, 8, 3, "Push / Pull / Legs|Набор массы"));
        var programs = new[]
        {
            (Id: -1, Names: new[] { "Всё тело A", "Всё тело B", "Всё тело C" }, Days: new[] { 1, 3, 5 }, Exercises: new[] { new[] { 2, 1, 8 }, new[] { 6, 5, 4 }, new[] { 7, 1, 8 } }),
            (Id: -2, Names: new[] { "Верх A", "Низ A", "Верх B", "Низ B" }, Days: new[] { 1, 2, 4, 5 }, Exercises: new[] { new[] { 1, 8, 5 }, new[] { 2, 6, 7 }, new[] { 5, 4, 1 }, new[] { 3, 7, 2 } }),
            (Id: -3, Names: new[] { "Присед и жим", "Тяга и плечи", "Силовая техника" }, Days: new[] { 1, 3, 5 }, Exercises: new[] { new[] { 2, 1, 8 }, new[] { 3, 5, 4 }, new[] { 2, 1, 6 } }),
            (Id: -4, Names: new[] { "Всё тело A", "Всё тело B", "Всё тело C" }, Days: new[] { 1, 3, 5 }, Exercises: new[] { new[] { 7, 1, 8 }, new[] { 6, 5, 4 }, new[] { 2, 1, 8 } }),
            (Id: -5, Names: new[] { "Push", "Pull", "Legs" }, Days: new[] { 1, 3, 5 }, Exercises: new[] { new[] { 1, 5 }, new[] { 8, 4, 3 }, new[] { 2, 6, 7 } })
        };
        var exerciseKey = -1;
        foreach (var p in programs)
        for (var i = 0; i < p.Names.Length; i++)
        {
            var workoutId = p.Id * 10 - i;
            model.Entity<ProgramWorkout>().HasData(new
            {
                Id = workoutId, TrainingProgramId = p.Id, Key = new Guid(Math.Abs(workoutId), 0, 0, new byte[8]),
                Name = p.Names[i], Order = i, DayOfWeek = (int?)p.Days[i], EstimatedMinutes = 50
            });
            for (var j = 0; j < p.Exercises[i].Length; j++)
            {
                var id = exerciseKey--;
                model.Entity<ProgramWorkoutExercise>().HasData(new { Id = id, ProgramWorkoutId = workoutId, ExerciseId = p.Exercises[i][j], Order = j });
                model.Entity<ProgramWorkoutExercise>().OwnsOne(e => e.Prescription).HasData(new
                {
                    ProgramWorkoutExerciseId = id, Sets = p.Id == -3 ? 4 : 3, RepsMin = p.Id == -3 ? 4 : 8,
                    RepsMax = p.Id == -3 ? 6 : 12, Rir = (int?)2, RestSeconds = (int?)120
                });
                model.Entity<ProgramWorkoutExercise>().OwnsOne(e => e.Progression).HasData(new
                {
                    ProgramWorkoutExerciseId = id, Method = ProgressionMethod.Manual, StepKg = 2.5m,
                    IncreasePercent = 5m, MinReps = 8, MaxReps = 12, TargetRir = 2, TargetRpe = 8m, OneRepMaxKg = 100m, PercentOneRepMax = 75m
                });
            }
        }
        model.Entity<ProgramBlock>().HasData(
            new ProgramBlock { Id = -1, TrainingProgramId = -5, StartWeek = 1, EndWeek = 3, Name = "Базовый объём" },
            new ProgramBlock { Id = -2, TrainingProgramId = -5, StartWeek = 4, EndWeek = 7, Name = "Рост нагрузки" },
            new ProgramBlock { Id = -3, TrainingProgramId = -5, StartWeek = 8, EndWeek = 8, Name = "Deload" });

        object Template(int id, string name, string goal, string description, ProgramLevel level, int weeks, int days, string categories, bool pro = false) => new
        {
            Id = id, Name = name, Goal = goal, Description = description, Level = level, Weeks = weeks, DaysPerWeek = days,
            Categories = categories, IsTemplate = true, RequiresPro = pro, IsArchived = false, CreatedAtUtc = created,
            Revision = new Guid(Math.Abs(id), 0, 0, new byte[8])
        };
    }
}
