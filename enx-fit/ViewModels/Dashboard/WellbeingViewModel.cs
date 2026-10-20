using System.Globalization;
using enx_fit.Models;
using enx_fit.Services.Dashboard;

namespace enx_fit.ViewModels.Dashboard;

public sealed record RhythmPoint(DateOnly Date, int Sessions, int RollingSessions, int? Minutes, bool Rest, int? Feeling);

// Observations from the journal and the user's own check-in. No inferred readiness score.
public sealed class WellbeingViewModel
{
    public static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    public DashboardData Data { get; }
    public DailyCheckIn Today => Data.TodayCheckIn;
    public IReadOnlyList<RhythmPoint> Path { get; }
    public DateOnly Month { get; }
    public DateOnly MonthEnd { get; }
    public IReadOnlyList<RhythmPoint> MonthDays { get; }
    public int Sessions => MonthDays.Sum(p => p.Sessions);
    public int ActiveDays => MonthDays.Count(p => p.Sessions > 0);
    public int PreviousSessions { get; }
    public int? Minutes { get; }
    public int MissingDurations { get; }
    public int WeeksWithActivity => Path.Chunk(7).Count(w => w.Any(p => p.Sessions > 0));
    public int RecentSessions => Data.CountCompleted(Data.Today.AddDays(-6), Data.Today);
    public bool HasHistory => Data.CompletedCount > 0;
    public string FeelingTitle => FeelingName(Today.Feeling);
    public string Headline => Today.RestDay ? "Сегодня можно выдохнуть."
        : Today.Feeling == 1 ? "Можно сбавить темп."
        : Data.CountCompleted(Data.Today, Data.Today) > 0 ? "Хорошая точка на сегодня."
        : !HasHistory ? "Начнём в вашем темпе."
        : RecentSessions == 0 ? "Всегда можно вернуться."
        : Today.Feeling == 3 ? "Пусть энергия станет движением."
        : "В своём ритме.";
    public string Message => Today.RestDay ? "Вы выбрали день отдыха. Он останется частью вашей истории, а план будет ждать, когда вы захотите продолжить."
        : Today.Feeling == 1 ? "Сегодня вы отметили меньше сил. Можно выбрать отдых или открыть план и решить, какой темп вам подходит."
        : Data.CountCompleted(Data.Today, Data.Today) > 0 ? "Сегодняшнее занятие уже в вашей истории. Не нужно добавлять ещё одно ради красивой цифры — ваш шаг уже сделан."
        : !HasHistory ? "Здесь будет виден ваш путь: занятия, паузы и возвращения. Начните с посильного шага — без гонки за непрерывной серией."
        : RecentSessions == 0 ? "Пауза не стирает сделанное. Посмотрите на свой путь и выберите следующий шаг, когда будете готовы."
        : Today.Feeling == 3 ? "Вы отметили, что сил достаточно. Ваш план рядом — выберите занятие, которое подходит вам сегодня."
        : $"За последние 7 дней в журнале {RecentSessions} {Plural(RecentSessions, "занятие", "занятия", "занятий")}. Отдых и движение могут чередоваться — важен ритм, который подходит вам.";

    public WellbeingViewModel(DashboardData data, DateOnly? month = null)
    {
        Data = data;
        Path = Enumerable.Range(0, 30).Select(i => Point(data.Today.AddDays(i - 29))).ToArray();
        var currentMonth = new DateOnly(data.Today.Year, data.Today.Month, 1);
        var requested = month ?? currentMonth;
        Month = new DateOnly(requested.Year, requested.Month, 1);
        if (Month > currentMonth) Month = currentMonth;
        if (Month < currentMonth.AddMonths(-11)) Month = currentMonth.AddMonths(-11);
        MonthEnd = Month.AddMonths(1).AddDays(-1);
        if (MonthEnd > data.Today) MonthEnd = data.Today;
        MonthDays = Enumerable.Range(0, MonthEnd.Day).Select(i => Point(Month.AddDays(i))).ToArray();
        var previous = Month.AddMonths(-1);
        var previousEnd = previous.AddDays(Math.Min(MonthEnd.Day, DateTime.DaysInMonth(previous.Year, previous.Month)) - 1);
        PreviousSessions = data.CountCompleted(previous, previousEnd);
        var sessions = data.Completed.Where(w => w.Date >= Month && w.Date <= MonthEnd).ToArray();
        var known = sessions.Where(w => w.DurationSeconds is > 0).ToArray();
        Minutes = known.Length > 0 ? (int)Math.Round(known.Sum(w => (long)w.DurationSeconds!.Value) / 60d) : null;
        MissingDurations = sessions.Length - known.Length;
    }

    private RhythmPoint Point(DateOnly date)
    {
        var sessions = Data.Completed.Where(w => w.Date == date).ToArray();
        var known = sessions.Where(w => w.DurationSeconds is > 0).ToArray();
        var check = Data.CheckIns.FirstOrDefault(c => c.Date == date);
        return new(date, Data.CountCompleted(date, date), Data.CountCompleted(date.AddDays(-6), date),
            known.Length > 0 ? (int)Math.Round(known.Sum(w => (long)w.DurationSeconds!.Value) / 60d) : null,
            check?.RestDay == true, check?.Feeling);
    }

    public static string FeelingName(int? feeling) => feeling switch { 1 => "Мало сил", 2 => "Всё ровно", 3 => "Много энергии", _ => "Как вы сегодня?" };
    public static string Plural(int n, string one, string few, string many) => n % 100 is >= 11 and <= 14 ? many : n % 10 == 1 ? one : n % 10 is >= 2 and <= 4 ? few : many;
}
