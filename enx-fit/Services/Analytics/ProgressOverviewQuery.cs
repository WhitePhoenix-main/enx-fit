using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Programs;
using enx_fit.Services.Workouts;
using enx_fit.ViewModels.Progress;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Analytics;

// Resolve the cohort before querying fitness data. Coach aggregates always use assigned clients.
public sealed class ProgressOverviewQuery(ApplicationDbContext db, CurrentUser user, IFeatureAccessService features,
    IProgramAnalysisService programAnalysis, IRecommendationService recommendations)
{
    public async Task<ProgressOverview?> LoadAsync(int requestedWeeks, string? clientId, int? programId,
        Guid? exerciseId, Guid? chartExercise, string metric, string clientMetric, bool personal, string? selectedClientId = null)
    {
        var access = await features.GetAsync();
        var coach = !personal && access.CanUse(Feature.CoachAnalytics);
        if (!coach && !string.IsNullOrEmpty(clientId) && clientId != user.Id) return null;
        var viewer = await db.Users.AsNoTracking().Where(u => u.Id == user.Id).Select(u => u.UserName ?? "Спортсмен").SingleAsync();
        var people = coach
            ? await db.Users.AsNoTracking().Where(u => u.TrainerId == user.Id && u.Id != user.Id)
                .OrderBy(u => u.UserName).Select(u => new ProgressClient(u.Id, u.UserName ?? "Клиент", "")).ToListAsync()
            : [new ProgressClient(user.Id, viewer, "")];
        if (!string.IsNullOrEmpty(clientId) && !people.Any(c => c.Id == clientId)) return null;
        var ids = people.Select(p => p.Id).ToArray();
        var settings = await db.DashboardSettings.AsNoTracking().Where(s => ids.Contains(s.UserId)).ToDictionaryAsync(s => s.UserId);
        people = people.Select(p => p with { Goal = settings.GetValueOrDefault(p.Id)?.GoalTitle ?? "Цель пока не указана" }).ToList();
        var weeks = requestedWeeks is 4 or 6 or 8 or 12 && (access.CanUse(Feature.AnalyticsAdvanced) || requestedWeeks <= 6)
            ? requestedWeeks : access.Plan == SubscriptionPlan.Pro ? 8 : 6;
        var today = user.LocalToday;
        var monday = ProgressOverview.Monday(today);
        var start = monday.AddDays(-7 * (weeks - 1));
        var plans = await db.TrainingPrograms.AsNoTracking().AsSplitQuery()
            .Where(p => p.OwnerId != null && ids.Contains(p.OwnerId) && !p.IsTemplate && !p.IsArchived)
            .Include(p => p.ScheduleChanges).Include(p => p.Blocks)
            .Include(p => p.Workouts).ThenInclude(w => w.Exercises).ThenInclude(e => e.Exercise).ToListAsync();
        if (programId.HasValue && !plans.Any(p => p.Id == programId)) return null;
        // Projection avoids loading mutable session entities or querying once per client.
        var sessions = await db.WorkoutSessions.AsNoTracking().Where(WorkoutStates.Completed)
            .Where(w => w.UserId != null && ids.Contains(w.UserId) && w.Date <= today)
            .OrderBy(w => w.Date).ThenBy(w => w.Id)
            .Select(w => new ProgressSession(w.Id, w.UserId!, w.Date, w.TrainingProgramId, w.ProgramWorkoutKey, w.ScheduledDate,
                w.WorkoutExercises.SelectMany(e => e.SetEntries.Where(s => s.IsCompleted && !s.IsSkipped && !s.IsWarmup && s.Reps > 0 &&
                    (s.IsBodyweight || s.Weight > 0 || s.Drops.Any(d => d.Reps > 0 && (d.Weight > 0 || d.IsBodyweight))))
                    .Select(s => new ProgressSet(e.ExerciseId, e.Exercise.Name, e.Exercise.MuscleGroup, s.Weight, s.Reps, s.IsBodyweight,
                        (s.IsBodyweight ? 0 : s.Weight * s.Reps) + s.Drops.Where(d => d.Reps > 0 && d.Weight > 0 && !d.IsBodyweight).Sum(d => d.Weight * d.Reps)))).ToList()))
            .ToListAsync();
        var completedSessions = sessions;
        sessions = sessions.Where(s => s.Sets.Count > 0).ToList();
        var measurements = await db.BodyMeasurements.AsNoTracking().Where(m => m.UserId != null && ids.Contains(m.UserId) && m.Date >= start && m.Date <= today)
            .OrderBy(m => m.Date).ThenBy(m => m.Id).ToListAsync();
        var checkIns = access.CanUse(Feature.AnalyticsAdvanced)
            ? await db.DailyCheckIns.AsNoTracking().Where(c => ids.Contains(c.UserId) && c.Date >= today.AddDays(-6) && c.Date <= today).ToListAsync() : [];
        var cohort = people.Where(p => clientId == null || p.Id == clientId)
            .Where(c => !programId.HasValue || plans.Any(p => p.Id == programId && p.OwnerId == c.Id)).ToList();
        if (programId.HasValue && cohort.Count == 0) return null;
        if (selectedClientId is not null && !cohort.Any(c => c.Id == selectedClientId)) return null;
        var selected = cohort.FirstOrDefault(c => c.Id == selectedClientId) ?? cohort.FirstOrDefault();
        var scoped = sessions.Where(s => cohort.Any(c => c.Id == s.UserId) && (!programId.HasValue || s.ProgramId == programId)).ToList();
        var scopedPlans = plans.Where(p => cohort.Any(c => c.Id == p.OwnerId) && (!programId.HasValue || p.Id == programId)).ToList();
        var choices = scoped.Where(s => !coach || s.UserId == selected?.Id).SelectMany(s => s.Sets).DistinctBy(s => s.ExerciseId).OrderBy(s => s.Name).ToList();
        if (exerciseId.HasValue && !choices.Any(e => e.ExerciseId == exerciseId)) exerciseId = null;
        var selectedExercise = exerciseId ?? chartExercise;
        if (!choices.Any(e => e.ExerciseId == selectedExercise)) selectedExercise = choices.FirstOrDefault()?.ExerciseId;
        var safeMetric = metric switch {
            "volume" when access.CanUse(Feature.AnalyticsTrainingVolume) => "volume",
            "1rm" or "reps" when access.CanUse(Feature.AnalyticsAdvanced) => metric,
            _ => "weight" };
        if (coach && clientMetric == "strength") safeMetric = "1rm";
        var peopleData = cohort.Select(c => BuildPerson(c, scoped.Where(s => s.UserId == c.Id).ToList(),
            measurements.Where(m => m.UserId == c.Id).ToList(), checkIns.Where(cn => cn.UserId == c.Id).ToList(),
            scopedPlans.Where(p => p.OwnerId == c.Id).ToList(), settings.GetValueOrDefault(c.Id), today, start, weeks,
            exerciseId, selectedExercise, safeMetric, clientMetric, access.CanUse(Feature.AnalyticsAdvanced),completedSessions)).ToList();
        var person = peopleData.FirstOrDefault(p => p.Client.Id == selected?.Id);
        if (!coach && person?.Program is { } current && access.CanUse(Feature.ProgressionRecommendations))
            person.Recommendation = await recommendations.NextForDashboardAsync(current.Program);
        var completions = scopedPlans.Select(p => Completion(p, completedSessions, today)).ToList();
        var currentVolume = scoped.Where(s => s.Date >= start).Sum(s => s.Sets.Sum(x => x.Volume));
        var prior = scoped.Where(s => s.Date < start && s.Date >= start.AddDays(-weeks * 7)).ToList();
        var recent = scoped.Where(s => s.Date >= start).ToList();
        var gains = peopleData.Where(p => p.StrengthGain.HasValue).Select(p => p.StrengthGain!.Value).ToList();
        var alerts = new List<ProgressAlert>();
        foreach (var c in cohort)
        {
            var history = scoped.Where(s => s.UserId == c.Id).ToList();
            var last = history.LastOrDefault()?.Date;
            if (last is null || last <= today.AddDays(-7)) alerts.Add(new(c.Id, c.Name, last is null ? "Пока нет завершённых тренировок" : $"Нет тренировок {(today.DayNumber - last.Value.DayNumber)} дней", true));
            var missed = scopedPlans.Where(p => p.OwnerId == c.Id).Sum(p => Completion(p, completedSessions, today).Missed);
            if (missed > 0) alerts.Add(new(c.Id, c.Name, $"Пропущено занятий по расписанию: {missed}", false));
        }
        return new() { Access=access, ViewerName=viewer.Split('@')[0], Today=today, Start=start, Weeks=weeks, Coach=coach,
            Clients=people, CohortCount=cohort.Count, Person=person,
            ClientActivity=cohort.Select(c=>new ProgressClientActivity(c.Id, scoped.Where(s=>s.UserId==c.Id).LastOrDefault()?.Date,
                scopedPlans.Where(p=>p.OwnerId==c.Id).Sum(p=>Completion(p,completedSessions,today).Missed))).ToArray(),
            Programs=plans.Where(p => clientId == null || p.OwnerId == clientId).Select(p => new ProgressChoice(p.Id.ToString(), p.Name)).ToArray(),
            Exercises=choices.Select(e => new ProgressChoice(e.ExerciseId.ToString(), e.Name)).ToArray(),
            ActiveClients=scoped.Where(s => s.Date >= monday).Select(s => s.UserId).Distinct().Count(),
            PriorActiveClients=scoped.Where(s => s.Date >= monday.AddDays(-7) && s.Date < monday).Select(s => s.UserId).Distinct().Count(),
            TeamStrengthGain=gains.Count == 0 ? null : Math.Round(gains.Average(), 1),
            TeamVolumeGain=ProgressOverview.Change(currentVolume, prior.Sum(s => s.Sets.Sum(x => x.Volume))),
            TeamAttendanceGain=ProgressOverview.Change(recent.Count, prior.Count),
            TeamChart=new("team", "Завершённые тренировки клиентов", "занятий", WeekPoints(start, weeks, d => scoped.Count(s => s.Date >= d && s.Date < d.AddDays(7))), true),
            TeamRecords=peopleData.SelectMany(p => p.Records.Select(r => r with { ClientId=p.Client.Id, ClientName=p.Client.Name })).Where(r => r.Gain > 0).OrderByDescending(r => r.Gain).ToArray(),
            Alerts=alerts, ProgramDone=completions.Sum(c => c.DoneDue), ProgramDue=completions.Sum(c => c.Due), ProgramMissed=completions.Sum(c => c.Missed),
            Goals=cohort.GroupBy(c => c.Goal).OrderByDescending(g => g.Count()).Select((g,i) => new ProgressSlice(g.Key,g.Count(),ProgressOverview.Colors[i % 6])).ToArray(),
            Attendance=peopleData.Select(p => (p.Client,p.WeekCounts)).ToArray() };
    }

    private ProgramCompletion Completion(TrainingProgram p, IReadOnlyList<ProgressSession> sessions, DateOnly today) => programAnalysis.Completion(p,
        sessions.Where(s => s.ProgramId == p.Id && s.UserId == p.OwnerId).Select(s => new WorkoutSession { Date=s.Date, ProgramWorkoutKey=s.WorkoutKey, ScheduledDate=s.ScheduledDate, Status=WorkoutStatus.Completed }).ToArray(), today);

    private ProgressPerson BuildPerson(ProgressClient client, List<ProgressSession> all, List<BodyMeasurement> body, List<DailyCheckIn> checkIns,
        List<TrainingProgram> plans, DashboardSettings? settings, DateOnly today, DateOnly start, int weeks, Guid? filter, Guid? chartId, string metric, string bodyMetric, bool advanced,IReadOnlyList<ProgressSession> completedSessions)
    {
        var monday=ProgressOverview.Monday(today);
        var period=all.Where(s => s.Date >= start).ToList();
        var filtered=period.SelectMany(s => s.Sets.Where(x => !filter.HasValue || x.ExerciseId == filter)).ToList();
        var before=all.Where(s => s.Date < start && s.Date >= start.AddDays(-7*weeks)).SelectMany(s => s.Sets.Where(x => !filter.HasValue || x.ExerciseId == filter)).ToList();
        var exercise=period.Where(s => s.Sets.Any(x => x.ExerciseId == chartId)).ToList();
        var rows=exercise.SelectMany(s => s.Sets.Where(x => x.ExerciseId == chartId)).ToList();
        var ownChoices=all.SelectMany(s => s.Sets).Where(s => !filter.HasValue || s.ExerciseId == filter).DistinctBy(s => s.ExerciseId).ToList();
        var records=new List<ProgressRecord>();
        foreach (var e in ownChoices)
        {
            var now=period.SelectMany(w => w.Sets.Where(s => s.ExerciseId == e.ExerciseId).Select(s => (w.Date, Set:s))).ToList();
            if(now.Count == 0) continue;
            var bodyweight=now.All(x => x.Set.Bodyweight);
            decimal Value(ProgressSet s) => bodyweight ? s.Reps : s.Bodyweight ? 0 : s.Weight;
            var best=now.OrderByDescending(x => Value(x.Set)).ThenByDescending(x => x.Date).First();
            var previous=all.Where(w => w.Date < start).SelectMany(w => w.Sets).Where(s => s.ExerciseId == e.ExerciseId).Select(Value).DefaultIfEmpty().Max();
            records.Add(new(e.ExerciseId,e.Name,Value(best.Set),bodyweight ? "повт." : "кг", previous > 0 ? Value(best.Set)-previous : null,best.Date));
        }
        decimal OneRm(ProgressSet s) => !s.Bodyweight && s.Weight > 0 ? s.Weight*(1+s.Reps/30m) : 0;
        var strength=new List<decimal>();
        if(advanced) foreach(var e in period.SelectMany(s => s.Sets).Select(s => s.ExerciseId).Distinct())
        {
            var h=period.Where(s => s.Sets.Any(x => x.ExerciseId==e && !x.Bodyweight && x.Weight>0)).GroupBy(s => s.Date)
                .Select(g => g.SelectMany(s => s.Sets).Where(x => x.ExerciseId==e).Max(OneRm)).ToList();
            if(h.Count>=2 && ProgressOverview.Change(h[^1],h[0]) is {} change) strength.Add(change);
        }
        var isBody=rows.Count>0 && rows.All(s => s.Bodyweight);
        if(isBody && metric=="weight") metric="reps";
        var points=exercise.GroupBy(s => s.Date).Select(g => {
            var values=g.SelectMany(s => s.Sets).Where(s => s.ExerciseId==chartId).ToArray();
            var value=metric switch { "volume" => values.Sum(s => s.Volume), "1rm" => values.Max(OneRm), "reps" => values.Max(s => (decimal)s.Reps), _ => values.Where(s => !s.Bodyweight).Select(s => s.Weight).DefaultIfEmpty().Max() };
            return new ProgressPoint(g.Key.ToString("d MMM", System.Globalization.CultureInfo.GetCultureInfo("ru-RU")),Math.Round(value,1),advanced && metric=="weight" ? Math.Round(values.Where(s => !s.Bodyweight).Select(s => s.Weight).DefaultIfEmpty().Average(),1) : null); }).ToList();
        var last=exercise.LastOrDefault();
        var current=last?.Sets.Where(s => s.ExerciseId==chartId).OrderByDescending(s => isBody?s.Reps:s.Weight).ThenByDescending(s => s.Reps).FirstOrDefault();
        var groups=filtered.GroupBy(s => PrimaryMuscle(s.Muscle)).OrderByDescending(g => g.Count()).ToList();
        var muscle=groups.Take(5).Select((g,i) => new ProgressSlice(g.Key,g.Count(),ProgressOverview.Colors[i])).ToList();
        if(groups.Count>5) muscle.Add(new("Другое",groups.Skip(5).Sum(g => g.Count()),ProgressOverview.Colors[5]));
        var weekCounts=Enumerable.Range(0,weeks).Select(i => period.Count(s => s.Date>=start.AddDays(i*7) && s.Date<start.AddDays((i+1)*7))).ToList();
        var streak=0; var cursor=monday;
        if(!all.Any(s=>s.Date>=cursor)) cursor=cursor.AddDays(-7);
        while(all.Any(s=>s.Date>=cursor && s.Date<cursor.AddDays(7))) { streak++;cursor=cursor.AddDays(-7); }
        var plan=plans.Where(p=>p.StartDate<=today && p.StartDate!.Value.AddDays(p.Weeks*7)>today).OrderByDescending(p=>p.StartDate).FirstOrDefault();
        ProgressProgram? program=null;
        if(plan?.StartDate is {} planStart) {
            var week=Math.Clamp((today.DayNumber-planStart.DayNumber)/7+1,1,plan.Weeks);
            var history=completedSessions.Where(s=>s.ProgramId==plan.Id&&s.UserId==client.Id).Select(s=>new WorkoutSession {Date=s.Date,ProgramWorkoutKey=s.WorkoutKey,ScheduledDate=s.ScheduledDate,Status=WorkoutStatus.Completed}).ToArray();
            program=new(plan,Completion(plan,completedSessions,today),week,plan.Blocks.FirstOrDefault(b=>b.StartWeek<=week&&b.EndWeek>=week)?.Name,ProgramSchedule.Next(plan,history,today));
        }
        var changeValue=points.Count>=2 ? points[^1].Value-points[0].Value : null;
        var label=metric switch {"volume"=>"Объём упражнения","1rm"=>"Оценка 1RM","reps"=>"Повторения",_=>"Рабочий вес"};
        var unit=metric=="reps" ? "повт." : "кг";
        var bodyValue=bodyMetric=="measurements" ? body.Where(b=>b.WaistCm.HasValue).Select(b=>new ProgressPoint(b.Date.ToString("d MMM"),b.WaistCm)).ToList()
            : body.Where(b=>b.WeightKg>0).Select(b=>new ProgressPoint(b.Date.ToString("d MMM"),b.WeightKg)).ToList();
        return new() {Client=client,ExerciseId=chartId,ExerciseName=current?.Name ?? ownChoices.FirstOrDefault(e=>e.ExerciseId==chartId)?.Name ?? "Выберите упражнение",Metric=metric,
            ExerciseChart=new("exercise",label,unit,points,false,advanced&&metric=="weight" ? "Средний рабочий вес":null),
            VolumeChart=new("volume","Общий объём тренировок","кг",WeekPoints(start,weeks,d=>period.Where(s=>s.Date>=d&&s.Date<d.AddDays(7)).SelectMany(s=>s.Sets).Where(s=>!filter.HasValue||s.ExerciseId==filter).Sum(s=>s.Volume))
                .Select((point,i)=>point with { Comparison=advanced ? all.Where(s=>s.Date>=start.AddDays(-weeks*7+i*7)&&s.Date<start.AddDays(-weeks*7+(i+1)*7)).SelectMany(s=>s.Sets).Where(s=>!filter.HasValue||s.ExerciseId==filter).Sum(s=>s.Volume) : null }).ToArray(),true,advanced?"Предыдущий период":null),
            BodyChart=new("body",bodyMetric=="measurements" ? "Талия" : "Вес тела",bodyMetric=="measurements" ? "см":"кг",bodyValue,ZeroBaseline:false),
            AttendanceChart=new("attendance","Посещаемость","занятий",WeekPoints(start,weeks,d=>period.Count(s=>s.Date>=d&&s.Date<d.AddDays(7))),true),
            Volume=filtered.Sum(s=>s.Volume),VolumeChange=ProgressOverview.Change(filtered.Sum(s=>s.Volume),before.Sum(s=>s.Volume)),
            StrengthGain=strength.Count==0?null:Math.Round(strength.Average(),1),Completed=period.Count,WeekCompleted=all.Count(s=>s.Date>=monday),
            WeeklyGoal=settings?.WeeklyWorkoutGoal??4,Streak=streak,ActiveWeeks=weekCounts.Count(c=>c>0),WeekCounts=weekCounts,
            WorkingSets=filtered.Count,ExerciseSets=rows.Count,Current=current,CurrentDate=last?.Date,ExerciseChange=changeValue,
            Initial=exercise.FirstOrDefault()?.Sets.Where(s=>s.ExerciseId==chartId).OrderByDescending(s=>isBody?s.Reps:s.Weight).ThenByDescending(s=>s.Reps).FirstOrDefault(),
            ExerciseSessions=exercise.Count,PreviousVolume=before.Sum(s=>s.Volume),
            BestValue=points.Count==0?null:points.Max(p=>p.Value),Unit=unit,Records=records.OrderByDescending(r=>r.Gain).ThenBy(r=>r.Name).ToList(),Muscles=muscle,
            Days=Enumerable.Range(0,7).Select(i=>(monday.AddDays(i),all.Count(s=>s.Date==monday.AddDays(i)))).ToArray(),Program=program,
            Measurement=body.LastOrDefault(),FirstMeasurement=body.Count>1?body.First():null,
            SleepHours=checkIns.Any(c=>c.SleepMinutes>0)?Math.Round(checkIns.Where(c=>c.SleepMinutes>0).Average(c=>(decimal)c.SleepMinutes)/60,1):null,
            Feeling=checkIns.LastOrDefault(c=>c.Feeling.HasValue)?.Feeling,RecoveryEntries=checkIns.Count(c=>c.Feeling.HasValue||c.SleepMinutes>0),
            Interpretation=points.Count<2 ? "Для сравнения нужны результаты как минимум за два дня. Запишите следующую тренировку — график обновится."
                : $"{label} в упражнении «{current?.Name}»: {ProgressOverview.Number(points[0].Value??0)} → {ProgressOverview.Number(points[^1].Value??0)} {unit} за выбранный период. Завершено тренировок с этим упражнением: {exercise.Count}." };
    }

    private static List<ProgressPoint> WeekPoints(DateOnly start,int weeks,Func<DateOnly,decimal> value) => Enumerable.Range(0,weeks)
        .Select(i=>new ProgressPoint(start.AddDays(i*7).ToString("d MMM",System.Globalization.CultureInfo.GetCultureInfo("ru-RU")),value(start.AddDays(i*7)))).ToList();
    private static string PrimaryMuscle(string muscle) => muscle.Split(',','/',';')[0].Trim() switch {
        "Квадрицепсы" or "Ягодицы" or "Бицепс бедра" or "Икры"=>"Ноги", "Бицепс" or "Трицепс"=>"Руки", "Пресс" or "Мышцы кора"=>"Кор", ""=>"Другое", var name=>name };
}
