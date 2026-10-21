using System.Globalization;
using enx_fit.Models;
using enx_fit.Presentation.Exercises;
using enx_fit.Services.Programs;
using enx_fit.Services.Workouts;

namespace enx_fit.ViewModels.Workouts;

public sealed record WorkoutHubExercise(Guid ExerciseId, string Name, string Prescription, string Result, int Total, int Done);

public sealed class WorkoutHubCard
{
    public WorkoutSession? Session { get; init; }
    public ProgramOccurrence? Occurrence { get; init; }
    public TrainingProgram? Program { get; init; }
    public string? ClientId { get; init; }
    public string? ClientName { get; init; }
    public required DateOnly Date { get; init; }
    public required DateOnly Today { get; init; }
    public string Title => Session?.Title ?? Occurrence?.Workout.Name ?? "Тренировка";
    public bool Active => Session?.IsActive == true;
    public bool Completed => Session?.State == WorkoutStatus.Completed && Date <= Today;
    public string Status => Active ? Session!.State==WorkoutStatus.Paused?"На паузе":"В процессе" : Completed?"Завершена":"По плану";
    public int Seconds => Session is null ? 0 : WorkoutStates.ElapsedSeconds(Session, DateTime.UtcNow);
    public int? Minutes => Completed ? Session!.DurationSeconds / 60 : Session?.PlannedMinutes ?? Occurrence?.Workout.EstimatedMinutes;
    public int ProgramWeek => Program?.StartDate is {} start ? Math.Clamp((Date.DayNumber-start.DayNumber)/7+1,1,Program.Weeks):0;
    public string ProgramLabel => Program is null ? "Самостоятельная тренировка" : Program.Name+(ProgramWeek>0?$" · Неделя {ProgramWeek} из {Program.Weeks}":"");
    public IReadOnlyList<WorkoutHubExercise> Exercises { get; init; } = [];
    public int Sets => Exercises.Sum(e=>e.Total);
    public int Done => Exercises.Sum(e=>e.Done);
    public int Percent => Sets==0?0:Math.Clamp((int)Math.Round(Done*100m/Sets),0,100);
    public decimal Volume => Session?.WorkoutExercises.SelectMany(e=>e.SetEntries).Where(Valid).Sum(s=>s.Volume)??0;
    public WorkoutHubExercise? Current => Exercises.FirstOrDefault(e=>e.Done<e.Total) ?? (Active?Exercises.LastOrDefault():null);
    public static bool Valid(SetEntry s)=>s.IsCompleted&&!s.IsSkipped&&!s.IsWarmup&&s.Reps>0;
    public static string N(decimal value)=>value.ToString("#,0.##",CultureInfo.GetCultureInfo("ru-RU"));
    public static string Initials(string name)=>string.Concat(name.Split(new[]{' ','.','_','-'},StringSplitOptions.RemoveEmptyEntries).Take(2).Select(s=>s[..1])).ToUpperInvariant();
    public static string Clock(int seconds)=>$"{seconds/60:00}:{seconds%60:00}";
    public static string Noun(int n,string one,string few,string many)=>n%100 is >=11 and <=14?many:n%10==1?one:n%10 is >=2 and <=4?few:many;
    public static string Count(int n,string one,string few,string many)=>$"{n} "+Noun(n,one,few,many);

    public static WorkoutHubCard Create(DateOnly today, WorkoutSession? session=null, ProgramOccurrence? occurrence=null, TrainingProgram? program=null, string? clientId=null,string? clientName=null)
    {
        var exercises=session is not null ? session.WorkoutExercises.OrderBy(e=>e.Order).Select(e=>
        {
            var sets=e.SetEntries.Where(s=>!s.IsWarmup).ToArray();
            var total=sets.Length>0?sets.Length:e.TargetSets??0;
            var valid=sets.Where(Valid).OrderByDescending(s=>s.Weight).ThenByDescending(s=>s.Reps).FirstOrDefault();
            var next=sets.OrderBy(s=>s.SetNumber).FirstOrDefault(s=>!s.IsCompleted&&!s.IsSkipped);
            var reps=e.TargetRepsMin?.ToString()??next?.Reps.ToString()??valid?.Reps.ToString()??"—";
            if(e.TargetRepsMax>e.TargetRepsMin)reps+="–"+e.TargetRepsMax;
            var weight=e.TargetWeightKg??next?.Weight;
            var prescription=$"{total} × {reps}"+(weight>0?$" · {N(weight.Value)} кг":"");
            var result=valid is null?"Нет рабочих подходов":valid.IsBodyweight?$"Свой вес × {valid.Reps}":$"{N(valid.Weight)} кг × {valid.Reps}";
            return new WorkoutHubExercise(e.ExerciseId,ExercisePresentation.DisplayName(e.Exercise),prescription,result,total,sets.Count(Valid));
        }).ToArray() : occurrence?.Workout.Exercises.OrderBy(e=>e.Order).Select(e=>new WorkoutHubExercise(e.ExerciseId,ExercisePresentation.DisplayName(e.Exercise),
            $"{e.Prescription.Sets} × {e.Prescription.RepsMin}"+(e.Prescription.RepsMax>e.Prescription.RepsMin?$"–{e.Prescription.RepsMax}":"")+(e.Prescription.WeightKg>0?$" · {N(e.Prescription.WeightKg.Value)} кг":""),"",e.Prescription.Sets,0)).ToArray()??[];
        return new(){Today=today,Date=session?.Date??occurrence?.Date??today,Session=session,Occurrence=occurrence,Program=program,ClientId=clientId,ClientName=clientName,Exercises=exercises};
    }
}
