using enx_fit.Models;
using enx_fit.Services;

namespace enx_fit.ViewModels;

public sealed record ProgramCalendarEntry(ProgramOccurrence Occurrence, WorkoutSession? Session, DateOnly Today)
{
    public string Status => Session?.State switch
    {
        WorkoutStatus.Completed => "Выполнена", WorkoutStatus.InProgress => "В процессе", WorkoutStatus.Paused => "На паузе",
        WorkoutStatus.Cancelled => "Отменена", _ => Occurrence.Date < Today ? "Пропущена" : Occurrence.Date == Today ? "Сегодня" : "По плану"
    };
    public bool CanMove => Session is null || (!Session.IsActive && !Session.IsFinished);
    public bool CanStart => Session?.State != WorkoutStatus.Completed;
}

public sealed class ProgramCalendarViewModel
{
    public DateOnly Monday { get; }
    public DateOnly MinDate { get; }
    public DateOnly MaxDate { get; }
    public IReadOnlyList<ProgramCalendarEntry> Entries { get; }
    public IReadOnlyList<DateOnly> Days => Enumerable.Range(0, 7).Select(i => Monday.AddDays(i)).ToArray();
    public bool HasPrevious => Monday > MondayOf(MinDate);
    public bool HasNext => Monday < MondayOf(MaxDate);
    public static DateOnly MondayOf(DateOnly date) => date.AddDays(-((int)date.DayOfWeek + 6) % 7);
    public ProgramCalendarViewModel(TrainingProgram program, IReadOnlyList<WorkoutSession> sessions, DateOnly today, DateOnly? week)
    {
        MinDate = program.StartDate ?? today;
        MaxDate = MinDate.AddDays(program.Weeks * 7 - 1);
        var proposed = week ?? today;
        if (proposed < MinDate) proposed = MinDate;
        if (proposed > MaxDate) proposed = MaxDate;
        Monday = MondayOf(proposed);
        Entries = ProgramSchedule.Occurrences(program).Select(o => new ProgramCalendarEntry(o,
            sessions.Where(s => ProgramSchedule.Matches(s, o)).OrderByDescending(s => s.State == WorkoutStatus.Completed)
                .ThenByDescending(s => s.IsActive).ThenByDescending(s => s.CreatedAtUtc).FirstOrDefault(), today)).ToArray();
    }
}
