using enx_fit.Security;
using enx_fit.Services.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using enx_fit.Services.Coaching;
using System.ComponentModel.DataAnnotations;

namespace enx_fit.Pages.Coaching;

[MinimumRole(UserRole.Trainer)]
public sealed class CalendarModel(SubscriptionDashboardQuery query, CurrentUser currentUser, IFeatureAccessService features,
    CoachCalendarService calendar, DashboardService dashboard) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? Day { get; set; }
    public IReadOnlyList<CoachScheduleCard> Schedule { get; private set; } = [];
    public DateOnly Today => currentUser.LocalToday;
    public DateOnly Monday => Day!.Value.AddDays(-((int)Day.Value.DayOfWeek + 6) % 7);
    public IReadOnlyList<CoachCalendarEntry> Entries { get; private set; } = [];
    public IReadOnlyList<ClientSummary> Clients { get; private set; } = [];
    public IReadOnlyList<CoachWorkoutChoice> Choices { get; private set; } = [];
    [BindProperty] public AppointmentInput Input { get; set; } = new();
    [TempData] public string? StatusMessage { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        if (!await features.CanUseAsync(Feature.CoachClientManagement)) return Forbid();
        if (Day is null || Day < Today.AddYears(-2) || Day > Today.AddYears(2)) Day = Today;
        Schedule = (await query.CoachAsync(Day.Value, fullSchedule: true)).Schedule;
        Entries = await calendar.WeekAsync(Monday);
        Clients = await dashboard.GetClientsAsync();
        Choices = await calendar.ChoicesAsync();
        if (Request.Method == "GET") Input.Date = Day.Value;
        return Page();
    }
    public async Task<IActionResult> OnPostAssignAsync()
    {
        if (!await features.CanUseAsync(Feature.CoachProgramAssignment)) return Forbid();
        if (ModelState.IsValid)
        {
            try
            {
                var id = await calendar.AssignAsync(Input.ClientId, Input.WorkoutId, Input.Date, Input.Time, Input.Minutes, Input.RequestId);
                if (id is null) return NotFound();
                StatusMessage = "Тренировка назначена. Занятие добавлено в план клиента.";
                return RedirectToPage(new { Day = Input.Date.ToString("yyyy-MM-dd") });
            }
            catch (ValidationException error) { ModelState.AddModelError("", error.Message); }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException) { ModelState.AddModelError("", "Расписание изменилось. Обновите страницу и повторите назначение."); }
        }
        return await OnGetAsync();
    }
    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        if (!await calendar.CancelAsync(id)) return NotFound();
        StatusMessage = "Запланированное занятие отменено.";
        return RedirectToPage(new { Day = Day?.ToString("yyyy-MM-dd") });
    }
    public sealed class AppointmentInput
    {
        [Required, StringLength(450)] public string ClientId { get; set; } = "";
        [Range(1, int.MaxValue)] public int WorkoutId { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; } = new(10, 0);
        [Range(15, 180)] public int Minutes { get; set; } = 60;
        public Guid RequestId { get; set; } = Guid.NewGuid();
    }
}
