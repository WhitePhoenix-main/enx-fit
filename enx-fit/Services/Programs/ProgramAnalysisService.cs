using enx_fit.Models;

namespace enx_fit.Services.Programs;

public sealed record ProgramCompletion(int Planned, int Done, int Due, int DoneDue, int Missed, DateOnly? Ends)
{
    public int Percent => Planned == 0 ? 0 : Math.Min(100, Done * 100 / Planned);
    public int? AdherencePercent => Due == 0 ? null : Math.Min(100, DoneDue * 100 / Due);
}

public interface IProgramAnalysisService
{
    ProgramCompletion Completion(TrainingProgram p, IReadOnlyList<WorkoutSession> sessions, DateOnly today);
}

// Factual completion is Free. Interpretation can be added without changing these metrics.
public sealed class ProgramAnalysisService : IProgramAnalysisService
{
    public ProgramCompletion Completion(TrainingProgram p, IReadOnlyList<WorkoutSession> sessions, DateOnly today)
    {
        var schedule = ProgramSchedule.Occurrences(p).ToList();
        bool Done(ProgramOccurrence o) => sessions.Any(s => ProgramSchedule.Matches(s, o) && s.State == WorkoutStatus.Completed);
        var due = schedule.Where(o => o.Date <= today).ToList();
        return new(schedule.Count, schedule.Count(Done), due.Count, due.Count(Done), schedule.Count(o => o.Date < today && !Done(o)),
            p.StartDate?.AddDays(p.Weeks * 7 - 1));
    }
}
