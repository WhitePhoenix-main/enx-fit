using System.ComponentModel.DataAnnotations;
using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Coaching;
using enx_fit.Services.Programs;
using enx_fit.Services.Workouts;
using enx_fit.ViewModels.Workouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Training;

[MinimumRole(UserRole.User)]
[RequestSizeLimit(16384)]
public sealed class IndexModel(ApplicationDbContext db, CurrentUser user, IFeatureAccessService features,
    WorkoutTemplateCatalog catalog, WorkoutService workouts, TrainingProgramService programService,
    IRecommendationService recommendations, CoachCalendarService coachCalendar) : PageModel
{
    [BindProperty(SupportsGet=true)] public string? View { get; set; }="train";
    [BindProperty(SupportsGet=true)] public bool Personal { get; set; }
    [BindProperty(SupportsGet=true)] public string? ClientId { get; set; }
    [BindProperty(SupportsGet=true)] public int? WorkoutId { get; set; }
    [BindProperty(SupportsGet=true)] public DateOnly? Day { get; set; }
    [BindProperty(SupportsGet=true)] public DateOnly? Month { get; set; }
    [BindProperty(SupportsGet=true),StringLength(120)] public string? Search { get; set; }
    [BindProperty(SupportsGet=true)] public int? Days { get; set; }
    [BindProperty(SupportsGet=true)] public DateOnly? Until { get; set; }
    [BindProperty] public enx_fit.Pages.Coaching.CalendarModel.AppointmentInput Appointment { get; set; }=new();
    public FeatureAccess Access { get; private set; }=null!;
    public bool Coach => !Personal&&Access.CanUse(Feature.CoachClientManagement);
    public string Badge=>Access.Plan.ToString();
    public string Theme=>Badge.ToLowerInvariant();
    public DateOnly Today=>user.LocalToday;
    public System.Globalization.CultureInfo Russian=>System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
    public DateOnly SelectedDay=>Day??Today;
    public DateOnly WeekStart=>SelectedDay.AddDays(-((int)SelectedDay.DayOfWeek+6)%7);
    public string ViewerName=>User.Identity?.Name?.Split('@')[0]??"Спортсмен";
    public IReadOnlyList<(string Id,string Name)> Clients { get; private set; }=[];
    public IReadOnlyList<WorkoutSession> Sessions { get; private set; }=[];
    public IReadOnlyList<ProgramOccurrence> Occurrences { get; private set; }=[];
    public IReadOnlyList<TrainingProgram> Programs { get; private set; }=[];
    public IReadOnlyList<WorkoutTemplateCard> Templates { get; private set; }=[];
    public IReadOnlyList<CoachWorkoutChoice> AssignmentChoices { get; private set; }=[];
    public IReadOnlyList<WorkoutHubCard> Agenda { get; private set; }=[];
    public IReadOnlyList<WorkoutHubCard> History { get; private set; }=[];
    public IReadOnlyList<WorkoutHubCard> SelectedDayCards { get; private set; }=[];
    public WorkoutHubCard? Detail { get; private set; }
    public WorkoutHubCard? Active { get; private set; }
    public WorkoutHubCard? Primary { get; private set; }
    public WorkoutHubCard? Upcoming { get; private set; }
    public TrainingProgram? CurrentProgram { get; private set; }
    public TrainingRecommendation? Recommendation { get; private set; }
    public string? RecommendationExercise { get; private set; }
    public string? TrainerNote { get; private set; }
    public int WeeklyDone { get; private set; }
    public int WeeklyGoal { get; private set; }
    public bool HasPrevious { get; private set; }
    public IReadOnlyList<DateOnly> HistoryMonths { get; private set; }=[];
    [TempData] public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Access=await features.GetAsync();
        View=View is "history" or "calendar" or "templates"?View:"train";
        if(Request.Method=="GET")ModelState.Clear();
        if(Day is null&&DateOnly.TryParse(Request.Query["SelectedDate"],out var selected))Day=selected;
        if(Day is null&&DateOnly.TryParse(Request.Query["WorkoutDate"],out var oldDay))Day=oldDay;
        if(Day is null&&int.TryParse(Request.Query["WeekOffset"],out var offset))Day=Today.AddDays(Math.Clamp(offset,-52,52)*7);
        if(Day<Today.AddYears(-20)||Day>Today.AddYears(5))Day=null;
        if(Month<Today.AddYears(-20)||Month>Today.AddYears(5))Month=null;
        if(View=="calendar"&&Day is null&&Month is {} requestedMonth&&(requestedMonth.Year!=Today.Year||requestedMonth.Month!=Today.Month))Day=new(requestedMonth.Year,requestedMonth.Month,1);
        Month=new((Month??SelectedDay).Year,(Month??SelectedDay).Month,1);
        if(WorkoutId is null&&int.TryParse(Request.Query["SelectedWorkout"],out var oldWorkout))WorkoutId=oldWorkout;
        ClientId=string.IsNullOrWhiteSpace(ClientId)?null:ClientId;
        Search=Search?.Trim();if(Search?.Length>120)Search=Search[..120];
        if(Coach)
        {
            var clients=await db.Users.AsNoTracking().Where(c=>c.TrainerId==user.Id&&c.Id!=user.Id).OrderBy(c=>c.UserName).Select(c=>new {c.Id,Name=c.UserName??"Клиент"}).ToArrayAsync();
            Clients=clients.Select(c=>(c.Id,c.Name)).ToArray();
            if(ClientId is not null&&!Clients.Any(c=>c.Id==ClientId))return NotFound();
            AssignmentChoices=await coachCalendar.ChoicesAsync();
        }
        else if(ClientId is not null&&ClientId!=user.Id)return NotFound();
        var ids=Coach?Clients.Where(c=>ClientId is null||c.Id==ClientId).Select(c=>c.Id).ToArray():new[]{user.Id};
        var gridStart=Month.Value.AddDays(-((int)Month.Value.DayOfWeek+6)%7);
        var from=new[]{gridStart,WeekStart,Today.AddDays(-7)}.Min();
        var to=new[]{gridStart.AddDays(41),WeekStart.AddDays(6),Today.AddDays(60)}.Max();
        var historyEnd=Until is {} until&&until<=Today&&until>=Today.AddYears(-20)?until:Today;
        var rangeStart=Days is 7 or 30 or 365?historyEnd.AddDays(1-Days.Value):Month.Value;
        var rangeEnd=Days is 7 or 30 or 365?historyEnd:Month.Value.AddMonths(1).AddDays(-1);
        from=from<rangeStart?from:rangeStart;to=to>rangeEnd?to:rangeEnd;
        Sessions=await db.WorkoutSessions.AsNoTracking().AsSplitQuery().Where(w=>w.UserId!=null&&ids.Contains(w.UserId)&&
            ((w.Date>=from&&w.Date<=to)||w.Id==WorkoutId||w.Status==WorkoutStatus.InProgress||w.Status==WorkoutStatus.Paused||w.Status==WorkoutStatus.Legacy&&w.StartedAtUtc!=null&&w.CompletedAtUtc==null))
            .Include(w=>w.WorkoutExercises).ThenInclude(e=>e.Exercise).Include(w=>w.WorkoutExercises).ThenInclude(e=>e.SetEntries)
            .OrderBy(w=>w.Date).ThenBy(w=>w.Id).ToArrayAsync();
        Programs=await db.TrainingPrograms.AsNoTracking().AsSplitQuery().Where(p=>p.OwnerId!=null&&ids.Contains(p.OwnerId)&&!p.IsTemplate)
            .Include(p=>p.ScheduleChanges).Include(p=>p.Workouts).ThenInclude(w=>w.Exercises).ThenInclude(e=>e.Exercise).ToArrayAsync();
        Occurrences=Programs.Where(p=>!p.IsArchived).SelectMany(ProgramSchedule.Occurrences).OrderBy(o=>o.Date).ToArray();
        WorkoutHubCard Card(WorkoutSession w)=>WorkoutHubCard.Create(Today,w,program:Programs.FirstOrDefault(p=>p.Id==w.TrainingProgramId),clientId:Coach?w.UserId:null,clientName:Clients.FirstOrDefault(c=>c.Id==w.UserId).Name);
        WorkoutHubCard Planned(ProgramOccurrence o)=>WorkoutHubCard.Create(Today,occurrence:o,program:Programs.First(p=>p.Id==o.Workout.TrainingProgramId));
        var live=Sessions.Where(w=>w.State!=WorkoutStatus.Cancelled).ToArray();
        if(WorkoutId.HasValue)
        {
            var selectedSession=live.SingleOrDefault(w=>w.Id==WorkoutId);
            if(selectedSession is null)return NotFound();
            Detail=Card(selectedSession);
            if(Coach)TrainerNote=await db.DashboardSettings.Where(s=>s.UserId==selectedSession.UserId).Select(s=>s.TrainerNote).FirstOrDefaultAsync();
        }
        History=live.Where(w=>w.State==WorkoutStatus.Completed&&w.Date<=Today&&w.Date>=rangeStart&&w.Date<=rangeEnd&&w.WorkoutExercises.Any(e=>e.SetEntries.Any(WorkoutHubCard.Valid))).Select(Card)
            .Where(w=>string.IsNullOrEmpty(Search)||w.Title.Contains(Search,StringComparison.OrdinalIgnoreCase)||w.Exercises.Any(e=>e.Name.Contains(Search,StringComparison.OrdinalIgnoreCase))||w.ClientName?.Contains(Search,StringComparison.OrdinalIgnoreCase)==true)
            .OrderByDescending(w=>w.Date).ThenByDescending(w=>w.Session!.Id).ToArray();
        var monthDates=await db.WorkoutSessions.AsNoTracking().Where(w=>w.UserId!=null&&ids.Contains(w.UserId)&&w.Date<=Today).Where(WorkoutStates.Completed).Select(w=>w.Date).Distinct().ToArrayAsync();
        HistoryMonths=monthDates.Select(d=>new DateOnly(d.Year,d.Month,1)).Append(new(Today.Year,Today.Month,1)).Append(Month.Value).Distinct().OrderDescending().ToArray();
        HasPrevious=await db.WorkoutSessions.Where(w=>w.UserId==user.Id&&w.Date<=Today).Where(WorkoutStates.Completed).AnyAsync();
        if(Coach)
        {
            Agenda=live.Where(w=>w.Date==SelectedDay).OrderBy(w=>w.ScheduledTime??TimeOnly.MaxValue).Select(Card).ToArray();
        }
        else
        {
            var settings=await db.DashboardSettings.AsNoTracking().SingleOrDefaultAsync(s=>s.UserId==user.Id);
            WeeklyGoal=settings?.WeeklyWorkoutGoal??3;
            var monday=Today.AddDays(-((int)Today.DayOfWeek+6)%7);
            WeeklyDone=live.Count(w=>w.State==WorkoutStatus.Completed&&w.Date>=monday&&w.Date<=Today&&w.WorkoutExercises.Any(e=>e.SetEntries.Any(WorkoutHubCard.Valid)));
            Active=live.Where(w=>w.IsActive).OrderByDescending(w=>w.CreatedAtUtc).Select(Card).FirstOrDefault();
            CurrentProgram=Programs.FirstOrDefault(p=>p.Id==Active?.Session?.TrainingProgramId)??Programs.Where(p=>!p.IsArchived&&p.StartDate<=Today&&p.StartDate.Value.AddDays(p.Weeks*7)>Today).OrderByDescending(p=>p.StartDate).FirstOrDefault();
            var daySessions=live.Where(w=>w.Date==SelectedDay).Select(Card);
            var dayPlans=Occurrences.Where(o=>o.Date==SelectedDay&&!live.Any(w=>ProgramSchedule.Matches(w,o))).Select(Planned);
            SelectedDayCards=daySessions.Concat(dayPlans).OrderByDescending(w=>w.Active).ThenBy(w=>w.Completed).ToArray();
            Primary=(SelectedDay==Today?Active:null)??SelectedDayCards.FirstOrDefault();
            Upcoming=live.Where(w=>w.Date>SelectedDay&&w.State==WorkoutStatus.Planned).Select(Card)
                .Concat(Occurrences.Where(o=>o.Date>SelectedDay&&!live.Any(w=>ProgramSchedule.Matches(w,o))).Select(Planned)).OrderBy(w=>w.Date).FirstOrDefault();
            if(Access.CanUse(Feature.ProgressionRecommendations)&&CurrentProgram is {} program&&!program.IsArchived)
            {
                Recommendation=await recommendations.NextForDashboardAsync(program);
                RecommendationExercise=program.Workouts.SelectMany(w=>w.Exercises).FirstOrDefault(e=>e.Id==Recommendation?.ProgramWorkoutExerciseId)?.Exercise.Name;
            }
        }
        Templates=await catalog.ListAsync();
        if(Request.Method=="GET")Appointment.Date=SelectedDay;
        Response.Headers.CacheControl="no-cache, no-store";
        return Page();
    }

    public object Values(string? view=null,DateOnly? day=null,DateOnly? month=null,int? workoutId=null,bool? personal=null)=>new { View=view??View,Day=(day??Day)?.ToString("yyyy-MM-dd"),Month=(month??Month)?.ToString("yyyy-MM-dd"),Personal=(personal??Personal)?(bool?)true:null,ClientId,WorkoutId=workoutId };
    public async Task<IActionResult> OnPostStartAsync(string source,int? templateId,int? plannedId,int? programId,int? previousId)
    {
        foreach(var key in ModelState.Keys.Where(k=>k.StartsWith("Appointment.")).ToArray())ModelState.Remove(key);
        if(!string.IsNullOrWhiteSpace(ClientId)&&ClientId!=user.Id)return NotFound();
        try
        {
            var id=source=="program"&&programId.HasValue?await programService.StartNextAsync(programId.Value):await workouts.StartAsync(source,templateId,plannedId,previousId);
            return RedirectToPage("/Training/Workouts/Details",new{id});
        }
        catch(InvalidOperationException error){ModelState.AddModelError("",error.Message);}
        catch(ProgramOperationException error){ModelState.AddModelError("",error.Message);}
        return await OnGetAsync();
    }
    public async Task<IActionResult> OnPostAssignAsync()
    {
        if(!await features.CanUseAsync(Feature.CoachProgramAssignment))return Forbid();
        if(ModelState.IsValid)try
        {
            var id=await coachCalendar.AssignAsync(Appointment.ClientId,Appointment.WorkoutId,Appointment.Date,Appointment.Time,Appointment.Minutes,Appointment.RequestId);
            if(id is null)return NotFound();
            StatusMessage="Тренировка назначена.";
            return RedirectToPage(new{Day=Appointment.Date.ToString("yyyy-MM-dd")});
        }
        catch(ValidationException error){ModelState.AddModelError("",error.Message);}
        catch(DbUpdateException){ModelState.AddModelError("","Расписание изменилось. Обновите страницу и повторите назначение.");}
        return await OnGetAsync();
    }
    public async Task<IActionResult> OnPostNoteAsync(int workoutId,string? note)
    {
        if(!await features.CanUseAsync(Feature.CoachClientManagement))return NotFound();
        var owner=await db.WorkoutSessions.Where(w=>w.Id==workoutId&&db.Users.Any(c=>c.Id==w.UserId&&c.TrainerId==user.Id&&c.Id!=user.Id)).Select(w=>w.UserId).SingleOrDefaultAsync();
        if(owner is null)return NotFound();
        if(note?.Length>1000)return BadRequest();
        var settings=await db.DashboardSettings.FindAsync(owner);
        if(settings is null){settings=new(){UserId=owner};db.DashboardSettings.Add(settings);}
        settings.TrainerNote=note?.Trim();await db.SaveChangesAsync();
        return RedirectToPage(new{WorkoutId=workoutId,Day=Day?.ToString("yyyy-MM-dd")});
    }
}
