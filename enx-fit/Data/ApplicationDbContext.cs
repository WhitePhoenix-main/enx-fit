using enx_fit.Models;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();

    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();

    public DbSet<SetEntry> SetEntries => Set<SetEntry>();

    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .IsRequired();

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.HasData(
                new Exercise { Id = 1, Name = "Bench Press" },
                new Exercise { Id = 2, Name = "Squat" },
                new Exercise { Id = 3, Name = "Deadlift" },
                new Exercise { Id = 4, Name = "Pull Up" },
                new Exercise { Id = 5, Name = "Overhead Press" },
                new Exercise { Id = 6, Name = "Romanian Deadlift" },
                new Exercise { Id = 7, Name = "Leg Press" },
                new Exercise { Id = 8, Name = "Barbell Row" });
        });

        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.Property(w => w.Title)
                .HasMaxLength(160);

            entity.Property(w => w.Notes)
                .HasMaxLength(1000);

            entity.HasMany(w => w.WorkoutExercises)
                .WithOne(w => w.WorkoutSession)
                .HasForeignKey(w => w.WorkoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutExercise>(entity =>
        {
            entity.HasOne(w => w.Exercise)
                .WithMany(e => e.WorkoutExercises)
                .HasForeignKey(w => w.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(w => w.SetEntries)
                .WithOne(s => s.WorkoutExercise)
                .HasForeignKey(s => s.WorkoutExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SetEntry>(entity =>
        {
            entity.Property(s => s.WeightKg)
                .HasPrecision(7, 2);

            entity.Property(s => s.Rpe)
                .HasPrecision(3, 1);

            entity.Property(s => s.Notes)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<BodyMeasurement>(entity =>
        {
            entity.Property(b => b.WeightKg)
                .HasPrecision(7, 2);

            entity.Property(b => b.BodyFatPercentage)
                .HasPrecision(5, 2);

            entity.Property(b => b.WaistCm)
                .HasPrecision(6, 2);

            entity.Property(b => b.ChestCm)
                .HasPrecision(6, 2);

            entity.Property(b => b.HipCm)
                .HasPrecision(6, 2);

            entity.Property(b => b.Notes)
                .HasMaxLength(1000);
        });
    }
}
