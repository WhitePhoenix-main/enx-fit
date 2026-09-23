using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace enx_fit.Migrations;

/// <inheritdoc />
public partial class EnrichBaseExercises : Migration
{
    private static readonly (int Id, string Group, string Equipment)[] Defaults =
    [
        (1, "Грудь", "Штанга, скамья"),
        (2, "Квадрицепсы", "Штанга"),
        (3, "Всё тело", "Штанга"),
        (4, "Спина", "Турник"),
        (5, "Плечи", "Штанга"),
        (6, "Задняя поверхность бедра", "Штанга"),
        (7, "Квадрицепсы", "Тренажёр для жима ногами"),
        (8, "Спина", "Штанга")
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep any names and metadata already customized by an administrator.
        foreach (var exercise in Defaults)
        {
            migrationBuilder.Sql($"""
                UPDATE "Exercises" SET "MuscleGroup" = '{exercise.Group}'
                WHERE "Id" = {exercise.Id} AND "MuscleGroup" = '';
                """);
            migrationBuilder.Sql($"""
                UPDATE "Exercises" SET "Equipment" = '{exercise.Equipment}'
                WHERE "Id" = {exercise.Id} AND "Equipment" = '';
                """);
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var exercise in Defaults)
        {
            migrationBuilder.Sql($"""
                UPDATE "Exercises" SET "MuscleGroup" = ''
                WHERE "Id" = {exercise.Id} AND "MuscleGroup" = '{exercise.Group}';
                """);
            migrationBuilder.Sql($"""
                UPDATE "Exercises" SET "Equipment" = ''
                WHERE "Id" = {exercise.Id} AND "Equipment" = '{exercise.Equipment}';
                """);
        }
    }
}
