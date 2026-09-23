using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace enx_fit.Migrations
{
    /// <inheritdoc />
    public partial class TrainingProgramsAndRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "WorkoutSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProgramWorkoutKey",
                table: "WorkoutSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ScheduledDate",
                table: "WorkoutSessions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainingProgramId",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetRepsMax",
                table: "WorkoutExercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetRepsMin",
                table: "WorkoutExercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetRir",
                table: "WorkoutExercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetRpe",
                table: "WorkoutExercises",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetSets",
                table: "WorkoutExercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetWeightKg",
                table: "WorkoutExercises",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionPlan",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TrainingPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Goal = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Weeks = table.Column<int>(type: "integer", nullable: false),
                    DaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    IsTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresPro = table.Column<bool>(type: "boolean", nullable: false),
                    Categories = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingPrograms_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignedPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgramId = table.Column<int>(type: "integer", nullable: false),
                    SourceProgramId = table.Column<int>(type: "integer", nullable: true),
                    TrainerId = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignedPrograms_AspNetUsers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignedPrograms_AspNetUsers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignedPrograms_TrainingPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "TrainingPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignedPrograms_TrainingPrograms_SourceProgramId",
                        column: x => x.SourceProgramId,
                        principalTable: "TrainingPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProgramBlock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingProgramId = table.Column<int>(type: "integer", nullable: false),
                    StartWeek = table.Column<int>(type: "integer", nullable: false),
                    EndWeek = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramBlock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramBlock_TrainingPrograms_TrainingProgramId",
                        column: x => x.TrainingProgramId,
                        principalTable: "TrainingPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramWorkout",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingProgramId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramWorkout", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramWorkout_TrainingPrograms_TrainingProgramId",
                        column: x => x.TrainingProgramId,
                        principalTable: "TrainingPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramWorkoutExercise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgramWorkoutId = table.Column<int>(type: "integer", nullable: false),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Prescription_Sets = table.Column<int>(type: "integer", nullable: false),
                    Prescription_RepsMin = table.Column<int>(type: "integer", nullable: false),
                    Prescription_RepsMax = table.Column<int>(type: "integer", nullable: false),
                    Prescription_WeightKg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    Prescription_PercentOneRepMax = table.Column<decimal>(type: "numeric", nullable: true),
                    Prescription_Rir = table.Column<int>(type: "integer", nullable: true),
                    Prescription_Rpe = table.Column<decimal>(type: "numeric", nullable: true),
                    Prescription_RestSeconds = table.Column<int>(type: "integer", nullable: true),
                    Prescription_Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Progression_Method = table.Column<int>(type: "integer", nullable: false),
                    Progression_StepKg = table.Column<decimal>(type: "numeric", nullable: false),
                    Progression_IncreasePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    Progression_MinReps = table.Column<int>(type: "integer", nullable: false),
                    Progression_MaxReps = table.Column<int>(type: "integer", nullable: false),
                    Progression_TargetRir = table.Column<int>(type: "integer", nullable: false),
                    Progression_TargetRpe = table.Column<decimal>(type: "numeric", nullable: false),
                    Progression_OneRepMaxKg = table.Column<decimal>(type: "numeric", nullable: false),
                    Progression_PercentOneRepMax = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramWorkoutExercise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramWorkoutExercise_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgramWorkoutExercise_ProgramWorkout_ProgramWorkoutId",
                        column: x => x.ProgramWorkoutId,
                        principalTable: "ProgramWorkout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TrainingProgramId = table.Column<int>(type: "integer", nullable: false),
                    ProgramWorkoutExerciseId = table.Column<int>(type: "integer", nullable: false),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProgramRevision = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    SuggestedChange = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    SuggestedValue = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRecommendations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingRecommendations_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingRecommendations_ProgramWorkoutExercise_ProgramWorko~",
                        column: x => x.ProgramWorkoutExerciseId,
                        principalTable: "ProgramWorkoutExercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingRecommendations_TrainingPrograms_TrainingProgramId",
                        column: x => x.TrainingProgramId,
                        principalTable: "TrainingPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TrainingPrograms",
                columns: new[] { "Id", "Categories", "CreatedAtUtc", "DaysPerWeek", "Description", "Goal", "IsArchived", "IsTemplate", "Level", "Name", "OwnerId", "RequiresPro", "Revision", "StartDate", "Weeks" },
                values: new object[,]
                {
                    { -5, "Push / Pull / Legs|Набор массы", new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Разделение по движениям с блоками объёма, нагрузки и облегчённой неделей.", "Набор мышечной массы", false, true, 2, "Push / Pull / Legs", null, false, new Guid("00000005-0000-0000-0000-000000000000"), null, 8 },
                    { -4, "Снижение веса|Для начинающих|Full Body", new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Умеренная силовая нагрузка трижды в неделю. Рабочие веса выбираются индивидуально.", "Снижение веса", false, true, 0, "Движение и тонус", null, false, new Guid("00000004-0000-0000-0000-000000000000"), null, 6 },
                    { -3, "Сила|Full Body", new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Три занятия в неделю с акцентом на основные силовые движения и восстановление.", "Развитие силы", false, true, 1, "Силовая база", null, false, new Guid("00000003-0000-0000-0000-000000000000"), null, 8 },
                    { -2, "Набор массы|Upper / Lower", new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Четыре занятия с чередованием верха и низа тела. Базовая структура для регулярных тренировок.", "Набор мышечной массы", false, true, 1, "Upper / Lower", null, false, new Guid("00000002-0000-0000-0000-000000000000"), null, 8 },
                    { -1, "Для начинающих|Full Body", new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Три занятия на всё тело. Освойте основные движения и подберите комфортную нагрузку.", "Общая физическая подготовка", false, true, 0, "Full Body · Первые шаги", null, false, new Guid("00000001-0000-0000-0000-000000000000"), null, 6 }
                });

            migrationBuilder.InsertData(
                table: "ProgramBlock",
                columns: new[] { "Id", "EndWeek", "Name", "StartWeek", "TrainingProgramId" },
                values: new object[,]
                {
                    { -3, 8, "Deload", 8, -5 },
                    { -2, 7, "Рост нагрузки", 4, -5 },
                    { -1, 3, "Базовый объём", 1, -5 }
                });

            migrationBuilder.InsertData(
                table: "ProgramWorkout",
                columns: new[] { "Id", "DayOfWeek", "EstimatedMinutes", "Key", "Name", "Order", "TrainingProgramId" },
                values: new object[,]
                {
                    { -52, 5, 50, new Guid("00000034-0000-0000-0000-000000000000"), "Legs", 2, -5 },
                    { -51, 3, 50, new Guid("00000033-0000-0000-0000-000000000000"), "Pull", 1, -5 },
                    { -50, 1, 50, new Guid("00000032-0000-0000-0000-000000000000"), "Push", 0, -5 },
                    { -42, 5, 50, new Guid("0000002a-0000-0000-0000-000000000000"), "Всё тело C", 2, -4 },
                    { -41, 3, 50, new Guid("00000029-0000-0000-0000-000000000000"), "Всё тело B", 1, -4 },
                    { -40, 1, 50, new Guid("00000028-0000-0000-0000-000000000000"), "Всё тело A", 0, -4 },
                    { -32, 5, 50, new Guid("00000020-0000-0000-0000-000000000000"), "Силовая техника", 2, -3 },
                    { -31, 3, 50, new Guid("0000001f-0000-0000-0000-000000000000"), "Тяга и плечи", 1, -3 },
                    { -30, 1, 50, new Guid("0000001e-0000-0000-0000-000000000000"), "Присед и жим", 0, -3 },
                    { -23, 5, 50, new Guid("00000017-0000-0000-0000-000000000000"), "Низ B", 3, -2 },
                    { -22, 4, 50, new Guid("00000016-0000-0000-0000-000000000000"), "Верх B", 2, -2 },
                    { -21, 2, 50, new Guid("00000015-0000-0000-0000-000000000000"), "Низ A", 1, -2 },
                    { -20, 1, 50, new Guid("00000014-0000-0000-0000-000000000000"), "Верх A", 0, -2 },
                    { -12, 5, 50, new Guid("0000000c-0000-0000-0000-000000000000"), "Всё тело C", 2, -1 },
                    { -11, 3, 50, new Guid("0000000b-0000-0000-0000-000000000000"), "Всё тело B", 1, -1 },
                    { -10, 1, 50, new Guid("0000000a-0000-0000-0000-000000000000"), "Всё тело A", 0, -1 }
                });

            migrationBuilder.InsertData(
                table: "ProgramWorkoutExercise",
                columns: new[] { "Id", "Prescription_Comment", "Prescription_PercentOneRepMax", "Prescription_RepsMax", "Prescription_RepsMin", "Prescription_RestSeconds", "Prescription_Rir", "Prescription_Rpe", "Prescription_Sets", "Prescription_WeightKg", "ExerciseId", "Order", "ProgramWorkoutId", "Progression_IncreasePercent", "Progression_MaxReps", "Progression_Method", "Progression_MinReps", "Progression_OneRepMaxKg", "Progression_PercentOneRepMax", "Progression_StepKg", "Progression_TargetRir", "Progression_TargetRpe" },
                values: new object[,]
                {
                    { -47, null, null, 12, 8, 120, 2, null, 3, null, 7, 2, -52, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -46, null, null, 12, 8, 120, 2, null, 3, null, 6, 1, -52, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -45, null, null, 12, 8, 120, 2, null, 3, null, 2, 0, -52, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -44, null, null, 12, 8, 120, 2, null, 3, null, 3, 2, -51, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -43, null, null, 12, 8, 120, 2, null, 3, null, 4, 1, -51, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -42, null, null, 12, 8, 120, 2, null, 3, null, 8, 0, -51, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -41, null, null, 12, 8, 120, 2, null, 3, null, 5, 1, -50, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -40, null, null, 12, 8, 120, 2, null, 3, null, 1, 0, -50, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -39, null, null, 12, 8, 120, 2, null, 3, null, 8, 2, -42, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -38, null, null, 12, 8, 120, 2, null, 3, null, 1, 1, -42, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -37, null, null, 12, 8, 120, 2, null, 3, null, 2, 0, -42, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -36, null, null, 12, 8, 120, 2, null, 3, null, 4, 2, -41, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -35, null, null, 12, 8, 120, 2, null, 3, null, 5, 1, -41, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -34, null, null, 12, 8, 120, 2, null, 3, null, 6, 0, -41, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -33, null, null, 12, 8, 120, 2, null, 3, null, 8, 2, -40, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -32, null, null, 12, 8, 120, 2, null, 3, null, 1, 1, -40, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -31, null, null, 12, 8, 120, 2, null, 3, null, 7, 0, -40, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -30, null, null, 6, 4, 120, 2, null, 4, null, 6, 2, -32, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -29, null, null, 6, 4, 120, 2, null, 4, null, 1, 1, -32, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -28, null, null, 6, 4, 120, 2, null, 4, null, 2, 0, -32, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -27, null, null, 6, 4, 120, 2, null, 4, null, 4, 2, -31, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -26, null, null, 6, 4, 120, 2, null, 4, null, 5, 1, -31, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -25, null, null, 6, 4, 120, 2, null, 4, null, 3, 0, -31, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -24, null, null, 6, 4, 120, 2, null, 4, null, 8, 2, -30, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -23, null, null, 6, 4, 120, 2, null, 4, null, 1, 1, -30, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -22, null, null, 6, 4, 120, 2, null, 4, null, 2, 0, -30, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -21, null, null, 12, 8, 120, 2, null, 3, null, 2, 2, -23, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -20, null, null, 12, 8, 120, 2, null, 3, null, 7, 1, -23, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -19, null, null, 12, 8, 120, 2, null, 3, null, 3, 0, -23, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -18, null, null, 12, 8, 120, 2, null, 3, null, 1, 2, -22, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -17, null, null, 12, 8, 120, 2, null, 3, null, 4, 1, -22, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -16, null, null, 12, 8, 120, 2, null, 3, null, 5, 0, -22, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -15, null, null, 12, 8, 120, 2, null, 3, null, 7, 2, -21, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -14, null, null, 12, 8, 120, 2, null, 3, null, 6, 1, -21, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -13, null, null, 12, 8, 120, 2, null, 3, null, 2, 0, -21, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -12, null, null, 12, 8, 120, 2, null, 3, null, 5, 2, -20, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -11, null, null, 12, 8, 120, 2, null, 3, null, 8, 1, -20, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -10, null, null, 12, 8, 120, 2, null, 3, null, 1, 0, -20, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -9, null, null, 12, 8, 120, 2, null, 3, null, 8, 2, -12, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -8, null, null, 12, 8, 120, 2, null, 3, null, 1, 1, -12, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -7, null, null, 12, 8, 120, 2, null, 3, null, 7, 0, -12, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -6, null, null, 12, 8, 120, 2, null, 3, null, 4, 2, -11, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -5, null, null, 12, 8, 120, 2, null, 3, null, 5, 1, -11, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -4, null, null, 12, 8, 120, 2, null, 3, null, 6, 0, -11, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -3, null, null, 12, 8, 120, 2, null, 3, null, 8, 2, -10, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -2, null, null, 12, 8, 120, 2, null, 3, null, 1, 1, -10, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m },
                    { -1, null, null, 12, 8, 120, 2, null, 3, null, 2, 0, -10, 5m, 12, 0, 8, 100m, 75m, 2.5m, 2, 8m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_TrainingProgramId_ProgramWorkoutKey_Schedul~",
                table: "WorkoutSessions",
                columns: new[] { "TrainingProgramId", "ProgramWorkoutKey", "ScheduledDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignedPrograms_ClientId",
                table: "AssignedPrograms",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedPrograms_ProgramId",
                table: "AssignedPrograms",
                column: "ProgramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignedPrograms_SourceProgramId",
                table: "AssignedPrograms",
                column: "SourceProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedPrograms_TrainerId",
                table: "AssignedPrograms",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramBlock_TrainingProgramId",
                table: "ProgramBlock",
                column: "TrainingProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramWorkout_TrainingProgramId_Key",
                table: "ProgramWorkout",
                columns: new[] { "TrainingProgramId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramWorkoutExercise_ExerciseId",
                table: "ProgramWorkoutExercise",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramWorkoutExercise_ProgramWorkoutId",
                table: "ProgramWorkoutExercise",
                column: "ProgramWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPrograms_OwnerId_IsArchived",
                table: "TrainingPrograms",
                columns: new[] { "OwnerId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecommendations_ExerciseId",
                table: "TrainingRecommendations",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecommendations_ProgramWorkoutExerciseId_EvidenceKey",
                table: "TrainingRecommendations",
                columns: new[] { "ProgramWorkoutExerciseId", "EvidenceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecommendations_TrainingProgramId",
                table: "TrainingRecommendations",
                column: "TrainingProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecommendations_UserId",
                table: "TrainingRecommendations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_TrainingPrograms_TrainingProgramId",
                table: "WorkoutSessions",
                column: "TrainingProgramId",
                principalTable: "TrainingPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_TrainingPrograms_TrainingProgramId",
                table: "WorkoutSessions");

            migrationBuilder.DropTable(
                name: "AssignedPrograms");

            migrationBuilder.DropTable(
                name: "ProgramBlock");

            migrationBuilder.DropTable(
                name: "TrainingRecommendations");

            migrationBuilder.DropTable(
                name: "ProgramWorkoutExercise");

            migrationBuilder.DropTable(
                name: "ProgramWorkout");

            migrationBuilder.DropTable(
                name: "TrainingPrograms");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_TrainingProgramId_ProgramWorkoutKey_Schedul~",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "ProgramWorkoutKey",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "TrainingProgramId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "TargetRepsMax",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "TargetRepsMin",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "TargetRir",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "TargetRpe",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "TargetSets",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "TargetWeightKg",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                table: "AspNetUsers");
        }
    }
}
