using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using enx_fit.Data;
using enx_fit.Models;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public sealed record ExercisePerformance(int SessionId, DateOnly Date, IReadOnlyList<PerformedSet> Sets,
    int? TargetSets, int? RepsMin, int? RepsMax, int? Rir, decimal? Rpe)
{
    public decimal EstimatedMax => Sets.Max(s => s.Weight * (1 + s.Reps / 30m));
    public int TotalReps => Sets.Sum(s => s.Reps);
}
public sealed record PerformedSet(int Number, decimal Weight, int Reps, int? Rir);

public sealed class ProgramTrainingDataService(ApplicationDbContext db, TrainingProgramService programs)
{
    public async Task<List<ExercisePerformance>> HistoryAsync(int programId, int programExerciseId)
    {
        // Resolve access on every read, including callers outside a Razor page.
        var p = await programs.FindAsync(programId) ?? throw new ProgramOperationException(ProgramFailure.NotFound);
        var workout = p.Workouts.SingleOrDefault(w => w.Exercises.Any(e => e.Id == programExerciseId));
        var exercise = workout?.Exercises.Single(e => e.Id == programExerciseId);
        if (exercise is null) throw new ProgramOperationException(ProgramFailure.NotFound);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await db.WorkoutExercises.AsNoTracking().Include(e => e.SetEntries).Include(e => e.WorkoutSession)
            .Where(e => e.WorkoutSession.UserId == p.OwnerId && e.WorkoutSession.TrainingProgramId == p.Id &&
                e.WorkoutSession.ProgramWorkoutKey == workout!.Key && e.ExerciseId == exercise.ExerciseId &&
                e.WorkoutSession.CompletedAtUtc != null && e.WorkoutSession.Date <= today)
            .OrderByDescending(e => e.WorkoutSession.Date).ThenByDescending(e => e.WorkoutSessionId).Take(12).ToListAsync();
        return rows.Where(e => e.SetEntries.Any(s => !s.IsWarmup && s.Reps > 0))
            .Select(e => new ExercisePerformance(e.WorkoutSessionId, e.WorkoutSession.Date,
                e.SetEntries.Where(s => !s.IsWarmup && s.Reps > 0).OrderBy(s => s.SetNumber).ThenBy(s => s.Id)
                    .Select(s => new PerformedSet(s.SetNumber, s.Weight, s.Reps, s.Rir)).ToList(),
                e.TargetSets, e.TargetRepsMin, e.TargetRepsMax, e.TargetRir, e.TargetRpe)).ToList();
    }

    public static string EvidenceKey(TrainingProgram p, ProgramWorkoutExercise e, IReadOnlyList<ExercisePerformance> history) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        { p.Revision, e.Id, e.Prescription, e.Progression, History = history.Take(4) }))));

    public static bool Comparable(ExercisePerformance performance, ExercisePrescription target) =>
        performance.Sets.Count == target.Sets && performance.Sets.Select(s => s.Number).Distinct().Count() == target.Sets &&
        performance.TargetSets == target.Sets && performance.RepsMin == target.RepsMin && performance.RepsMax == target.RepsMax &&
        performance.Sets.All(s => s.Weight == target.WeightKg && s.Weight > 0 && s.Reps is > 0 and <= 30);
}
