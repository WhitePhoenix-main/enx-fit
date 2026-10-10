using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class StarterProgramsAndTechniqueExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Exercises",
                columns: new[] { "Id", "Equipment", "MuscleGroup", "Name", "Notes" },
                values: new object[] { new Guid("e71c15e5-0000-4000-8000-0000fffffbfe"), "Без оборудования", "Грудь", "Отжимания с колен", null });

            migrationBuilder.InsertData(
                table: "TrainingPrograms",
                columns: new[] { "Id", "Categories", "CreatedAtUtc", "DaysPerWeek", "Description", "Goal", "IsArchived", "IsTemplate", "Level", "Name", "OwnerId", "RequiresPro", "Revision", "StartDate", "Weeks" },
                values: new object[,]
                {
                    { -7, "Для начинающих|Дом|Гантели", new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Четыре движения с парой гантелей и собственным весом. Скамья не нужна; нагрузку выбираете сами.", "Общая физическая подготовка", false, true, 0, "Гантели · Два занятия", null, false, new Guid("00000007-0000-0000-0000-000000000000"), null, 4 },
                    { -6, "Для начинающих|Дом", new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Три движения с собственным весом, два коротких занятия в неделю. Повторяйте знакомые упражнения и осваивайте запись подходов.", "Регулярность", false, true, 0, "Дом · Первые движения", null, false, new Guid("00000006-0000-0000-0000-000000000000"), null, 4 }
                });

            migrationBuilder.InsertData(
                table: "ProgramWorkout",
                columns: new[] { "Id", "DayOfWeek", "EstimatedMinutes", "Key", "Name", "Order", "TrainingProgramId" },
                values: new object[,]
                {
                    { -71, 4, 25, new Guid("00000047-0000-0000-0000-000000000000"), "Закрепить движения", 1, -7 },
                    { -70, 1, 25, new Guid("00000046-0000-0000-0000-000000000000"), "Знакомство с гантелями", 0, -7 },
                    { -61, 4, 20, new Guid("0000003d-0000-0000-0000-000000000000"), "Повторить знакомое", 1, -6 },
                    { -60, 1, 20, new Guid("0000003c-0000-0000-0000-000000000000"), "Освоить движения", 0, -6 }
                });

            migrationBuilder.InsertData(
                table: "ProgramWorkoutExercise",
                columns: new[] { "Id", "Prescription_Comment", "Prescription_PercentOneRepMax", "Prescription_RepsMax", "Prescription_RepsMin", "Prescription_RestSeconds", "Prescription_Rir", "Prescription_Rpe", "Prescription_Sets", "Prescription_WeightKg", "ExerciseId", "Order", "ProgramWorkoutId", "Progression_IncreasePercent", "Progression_MaxReps", "Progression_Method", "Progression_MinReps", "Progression_OneRepMaxKg", "Progression_PercentOneRepMax", "Progression_StepKg", "Progression_TargetRir", "Progression_TargetRpe" },
                values: new object[,]
                {
                    { -61, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffff8f6"), 3, -71, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -60, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffffbfe"), 2, -71, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -59, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffffba6"), 1, -71, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -58, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffff9bc"), 0, -71, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -57, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffff8f6"), 3, -70, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -56, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffffbfe"), 2, -70, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -55, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffffba6"), 1, -70, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -54, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffff9bc"), 0, -70, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -53, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffff8f6"), 2, -61, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -52, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffffbfe"), 1, -61, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -51, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffff9ba"), 0, -61, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -50, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffff8f6"), 2, -60, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -49, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffffbfe"), 1, -60, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -48, "Начните с комфортного числа повторений. Можно уменьшить число подходов; при потере техники остановитесь.", null, 12, 8, 90, null, null, 2, null, new Guid("e71c15e5-0000-4000-8000-0000fffff9ba"), 0, -60, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -61);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -60);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -59);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -58);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -57);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -56);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -55);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -54);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -53);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -52);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -51);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -50);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -49);

            migrationBuilder.DeleteData(
                table: "ProgramWorkoutExercise",
                keyColumn: "Id",
                keyValue: -48);

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("e71c15e5-0000-4000-8000-0000fffffbfe"));

            migrationBuilder.DeleteData(
                table: "ProgramWorkout",
                keyColumn: "Id",
                keyValue: -71);

            migrationBuilder.DeleteData(
                table: "ProgramWorkout",
                keyColumn: "Id",
                keyValue: -70);

            migrationBuilder.DeleteData(
                table: "ProgramWorkout",
                keyColumn: "Id",
                keyValue: -61);

            migrationBuilder.DeleteData(
                table: "ProgramWorkout",
                keyColumn: "Id",
                keyValue: -60);

            migrationBuilder.DeleteData(
                table: "TrainingPrograms",
                keyColumn: "Id",
                keyValue: -7);

            migrationBuilder.DeleteData(
                table: "TrainingPrograms",
                keyColumn: "Id",
                keyValue: -6);
        }
    }
}
