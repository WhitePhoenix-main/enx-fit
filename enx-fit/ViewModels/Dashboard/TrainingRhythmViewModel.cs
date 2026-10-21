using enx_fit.Services.Dashboard;

namespace enx_fit.ViewModels.Dashboard;

public sealed record TrainingRhythmDay(DateOnly Date, int Count);

public sealed class TrainingRhythmViewModel
{
    public IReadOnlyList<TrainingRhythmDay> Days { get; }
    public int ActiveDays => Days.Count(d => d.Count > 0);
    public int Sessions => Days.Sum(d => d.Count);
    public int ActiveWeeks => Days.Chunk(7).Count(week => week.Any(d => d.Count > 0));
    public int RecentSessions => Days.TakeLast(7).Sum(d => d.Count);
    public int PriorSessions => Days.Skip(14).Take(7).Sum(d => d.Count);
    public string Message => Sessions == 0 ? "Начните с одного занятия. Ваш ритм появится постепенно."
        : RecentSessions == 0 ? "Пауза — часть пути. Вернитесь к плану, когда будете готовы."
        : RecentSessions > PriorSessions ? "За последние 7 дней занятий больше, чем за предыдущие. Оставляйте время для отдыха."
        : "Вы находите время для движения. Регулярность складывается из небольших шагов.";

    public TrainingRhythmViewModel(DashboardData data)
    {
        Days = Enumerable.Range(0, 28).Select(i => data.Today.AddDays(i - 27))
            .Select(date => new TrainingRhythmDay(date, data.CountCompleted(date, date))).ToArray();
    }
}
