using System.Globalization;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Programs;

namespace enx_fit.ViewModels.Progress;

public sealed record ProgressSet(Guid ExerciseId, string Name, string Muscle, decimal Weight, int Reps, bool Bodyweight, decimal Volume);
public sealed record ProgressSession(int Id, string UserId, DateOnly Date, int? ProgramId, Guid? WorkoutKey, DateOnly? ScheduledDate, List<ProgressSet> Sets);
public sealed record ProgressClient(string Id, string Name, string Goal);
public sealed record ProgressClientActivity(string ClientId, DateOnly? LastDate, int Missed);
public sealed record ProgressChoice(string Id, string Name);
public sealed record ProgressPoint(string Label, decimal? Value, decimal? Comparison = null);
public sealed record ProgressChart(string Id, string Label, string Unit, IReadOnlyList<ProgressPoint> Points, bool Bars = false, string? ComparisonLabel = null, bool ZeroBaseline = true);
public sealed record ProgressSlice(string Name, int Count, string Color);
public sealed record ProgressRecord(Guid ExerciseId, string Name, decimal Value, string Unit, decimal? Gain, DateOnly Date, string? ClientId = null, string? ClientName = null);
public sealed record ProgressAlert(string ClientId, string Name, string Reason, bool Urgent);
public sealed record ProgressProgram(TrainingProgram Program, ProgramCompletion Completion, int Week, string? Phase, ProgramOccurrence? Next);

public sealed class ProgressOverview
{
    public required FeatureAccess Access { get; init; }
    public required string ViewerName { get; init; }
    public required DateOnly Today { get; init; }
    public required DateOnly Start { get; init; }
    public required int Weeks { get; init; }
    public bool Coach { get; init; }
    public bool Advanced => Access.CanUse(Feature.AnalyticsAdvanced);
    public string Theme => Access.Plan.ToString().ToLowerInvariant();
    public string Badge => Access.Plan.ToString();
    public IReadOnlyList<ProgressClient> Clients { get; init; } = [];
    public IReadOnlyList<ProgressClientActivity> ClientActivity { get; init; } = [];
    public IReadOnlyList<ProgressChoice> Programs { get; init; } = [];
    public IReadOnlyList<ProgressChoice> Exercises { get; init; } = [];
    public ProgressPerson? Person { get; init; }
    public int ActiveClients { get; init; }
    public int PriorActiveClients { get; init; }
    public int CohortCount { get; init; }
    public int ActivePercent => Percent(ActiveClients, CohortCount);
    public int ActivityChange => ActivePercent - Percent(PriorActiveClients, CohortCount);
    public decimal? TeamStrengthGain { get; init; }
    public decimal? TeamVolumeGain { get; init; }
    public decimal? TeamAttendanceGain { get; init; }
    public required ProgressChart TeamChart { get; init; }
    public IReadOnlyList<ProgressRecord> TeamRecords { get; init; } = [];
    public IReadOnlyList<ProgressAlert> Alerts { get; init; } = [];
    public IReadOnlyList<ProgressSlice> Goals { get; init; } = [];
    public IReadOnlyList<(ProgressClient Client, IReadOnlyList<int> Counts)> Attendance { get; init; } = [];
    public int ProgramDone { get; init; }
    public int ProgramDue { get; init; }
    public int ProgramMissed { get; init; }
    public int ProgramPercent => Percent(ProgramDone, ProgramDue);
    public static int Percent(int value, int total) => total <= 0 ? 0 : Math.Clamp((int)Math.Round(value * 100m / total), 0, 100);
    public static string Number(decimal value) => value.ToString("#,#0.##", CultureInfo.GetCultureInfo("ru-RU"));
    public static string Signed(decimal? value, string unit = "%") => value is null ? "—" : (value > 0 ? "+" : "") + Number(value.Value) + unit;
    public static decimal? Change(decimal current, decimal previous) => previous <= 0 ? null : Math.Round((current - previous) / previous * 100, 1);
    public static DateOnly Monday(DateOnly date) => date.AddDays(-((int)date.DayOfWeek + 6) % 7);
    public static readonly string[] Colors = ["#1688ff", "#974cff", "#00c5ad", "#ffb339", "#ff485e", "#7797c8"];
}

public sealed class ProgressPerson
{
    public required ProgressClient Client { get; init; }
    public required Guid? ExerciseId { get; init; }
    public required string ExerciseName { get; init; }
    public required string Metric { get; init; }
    public required ProgressChart ExerciseChart { get; init; }
    public required ProgressChart VolumeChart { get; init; }
    public required ProgressChart BodyChart { get; init; }
    public required ProgressChart AttendanceChart { get; init; }
    public decimal Volume { get; init; }
    public decimal? VolumeChange { get; init; }
    public decimal? StrengthGain { get; init; }
    public int Completed { get; init; }
    public int WeekCompleted { get; init; }
    public int WeeklyGoal { get; init; }
    public int Streak { get; init; }
    public int ActiveWeeks { get; init; }
    public int WorkingSets { get; init; }
    public int ExerciseSets { get; init; }
    public ProgressSet? Current { get; init; }
    public ProgressSet? Initial { get; init; }
    public int ExerciseSessions { get; init; }
    public decimal PreviousVolume { get; init; }
    public string CurrentLabel => Metric switch { "volume" => "Объём за занятие", "1rm" => "Оценка 1RM", "reps" => "Повторения", _ => "Текущий результат" };
    public string CurrentDisplay => Current is null ? "—" : Metric=="weight"
        ? ProgressOverview.Number(Current.Weight)+" кг × "+Current.Reps
        : ExerciseChart.Points.LastOrDefault()?.Value is {} value ? ProgressOverview.Number(value)+" "+Unit : "—";
    public DateOnly? CurrentDate { get; init; }
    public decimal? ExerciseChange { get; init; }
    public decimal? BestValue { get; init; }
    public string Unit { get; init; } = "кг";
    public IReadOnlyList<ProgressRecord> Records { get; init; } = [];
    public IReadOnlyList<ProgressSlice> Muscles { get; init; } = [];
    public IReadOnlyList<(DateOnly Date, int Count)> Days { get; init; } = [];
    public IReadOnlyList<int> WeekCounts { get; init; } = [];
    public ProgressProgram? Program { get; init; }
    public TrainingRecommendation? Recommendation { get; set; }
    public BodyMeasurement? Measurement { get; init; }
    public BodyMeasurement? FirstMeasurement { get; init; }
    public decimal? SleepHours { get; init; }
    public int? Feeling { get; init; }
    public int RecoveryEntries { get; init; }
    public string Interpretation { get; init; } = "Добавьте завершённые тренировки, чтобы увидеть динамику.";
}
