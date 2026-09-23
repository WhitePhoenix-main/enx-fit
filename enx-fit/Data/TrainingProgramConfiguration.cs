using enx_fit.Areas.Identity.Data;
using enx_fit.Models;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Data;

internal static class TrainingProgramConfiguration
{
    public static void ConfigureTrainingPrograms(this ModelBuilder model)
    {
        model.Entity<TrainingProgram>(p =>
        {
            p.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
            p.HasIndex(x => new { x.OwnerId, x.IsArchived });
            p.Property(x => x.Revision).IsConcurrencyToken();
            p.HasMany(x => x.Workouts).WithOne().HasForeignKey(x => x.TrainingProgramId).OnDelete(DeleteBehavior.Cascade);
            p.HasMany(x => x.Blocks).WithOne().HasForeignKey(x => x.TrainingProgramId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<ProgramWorkout>(w =>
        {
            w.HasIndex(x => new { x.TrainingProgramId, x.Key }).IsUnique();
            w.HasMany(x => x.Exercises).WithOne().HasForeignKey(x => x.ProgramWorkoutId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<ProgramWorkoutExercise>(e =>
        {
            e.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
            e.OwnsOne(x => x.Prescription, p => { p.Ignore(x => x.Summary); p.Property(x => x.WeightKg).HasPrecision(8, 2); });
            e.OwnsOne(x => x.Progression);
        });
        model.Entity<AssignedProgram>(a =>
        {
            a.HasOne(x => x.Program).WithOne(x => x.Assignment).HasForeignKey<AssignedProgram>(x => x.ProgramId).OnDelete(DeleteBehavior.Cascade);
            a.HasOne<TrainingProgram>().WithMany().HasForeignKey(x => x.SourceProgramId).OnDelete(DeleteBehavior.SetNull);
            a.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Cascade);
            a.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<WorkoutSession>(s =>
        {
            s.HasOne<TrainingProgram>().WithMany().HasForeignKey(x => x.TrainingProgramId).OnDelete(DeleteBehavior.SetNull);
            s.HasIndex(x => new { x.TrainingProgramId, x.ProgramWorkoutKey, x.ScheduledDate }).IsUnique();
        });
        model.Entity<TrainingRecommendation>(r =>
        {
            r.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            r.HasOne<TrainingProgram>().WithMany().HasForeignKey(x => x.TrainingProgramId).OnDelete(DeleteBehavior.Cascade);
            r.HasOne<ProgramWorkoutExercise>().WithMany().HasForeignKey(x => x.ProgramWorkoutExerciseId).OnDelete(DeleteBehavior.Cascade);
            r.HasOne<Exercise>().WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
            r.HasIndex(x => new { x.ProgramWorkoutExerciseId, x.EvidenceKey }).IsUnique();
            r.Property(x => x.Status).IsConcurrencyToken();
            r.Property(x => x.CurrentValue).HasPrecision(8, 2);
            r.Property(x => x.SuggestedValue).HasPrecision(8, 2);
        });
        ProgramTemplateCatalog.Seed(model);
    }
}
