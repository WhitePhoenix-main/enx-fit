using enx_fit.Areas.Identity.Data;
using enx_fit.Models;
using enx_fit.Security;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
    public DbSet<AssignedProgram> AssignedPrograms => Set<AssignedProgram>();
    public DbSet<TrainingRecommendation> TrainingRecommendations => Set<TrainingRecommendation>();

    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();

    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();

    public DbSet<SetEntry> SetEntries => Set<SetEntry>();

    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<DashboardSettings> DashboardSettings => Set<DashboardSettings>();
    public DbSet<DailyCheckIn> DailyCheckIns => Set<DailyCheckIn>();
    public DbSet<DashboardLayoutPreference> DashboardLayoutPreferences => Set<DashboardLayoutPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureTrainingPrograms();

        modelBuilder.Entity<ApplicationUser>(user =>
        {
            user.HasOne(item => item.Trainer).WithMany().HasForeignKey(item => item.TrainerId)
                .OnDelete(DeleteBehavior.SetNull);
            user.Property(item => item.Role)
                .HasConversion<int>()
                .HasDefaultValue(UserRole.User)
                .HasSentinel(UserRole.User);
            user.ToTable("AspNetUsers", table => table.HasCheckConstraint(
                "CK_AspNetUsers_Role", "\"Role\" IN (0, 1, 3, 5, 8, 9)"));
        });

        modelBuilder.Entity<DashboardSettings>(entity =>
        {
            entity.HasKey(item => item.UserId);
            entity.Property(item => item.GoalTitle).HasMaxLength(120);
            entity.Property(item => item.TrainerNote).HasMaxLength(1000);
            entity.HasOne<ApplicationUser>().WithOne().HasForeignKey<DashboardSettings>(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DailyCheckIn>(entity =>
        {
            entity.HasKey(item => new { item.UserId, item.Date });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DashboardLayoutPreference>(entity =>
        {
            entity.HasKey(item => new { item.UserId, item.Mode });
            entity.Property(item => item.Mode).HasMaxLength(16);
            entity.Property(item => item.WidgetsJson).HasMaxLength(6000);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .IsRequired();

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.HasData(
                new Exercise { Id = 1, Name = "Bench Press", MuscleGroup = "Грудь", Equipment = "Штанга, скамья" },
                new Exercise { Id = 2, Name = "Squat", MuscleGroup = "Квадрицепсы", Equipment = "Штанга" },
                new Exercise { Id = 3, Name = "Deadlift", MuscleGroup = "Всё тело", Equipment = "Штанга" },
                new Exercise { Id = 4, Name = "Pull Up", MuscleGroup = "Спина", Equipment = "Турник" },
                new Exercise { Id = 5, Name = "Overhead Press", MuscleGroup = "Плечи", Equipment = "Штанга" },
                new Exercise { Id = 6, Name = "Romanian Deadlift", MuscleGroup = "Задняя поверхность бедра", Equipment = "Штанга" },
                new Exercise { Id = 7, Name = "Leg Press", MuscleGroup = "Квадрицепсы", Equipment = "Тренажёр для жима ногами" },
                new Exercise { Id = 8, Name = "Barbell Row", MuscleGroup = "Спина", Equipment = "Штанга" });
            entity.HasData(ExerciseCatalog.All);
        });

        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.Property(w => w.UserId)
                .HasMaxLength(450);

            entity.HasIndex(w => w.UserId);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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

        /*modelBuilder.Entity<SetEntry>(entity =>
        {
            entity.Property(s => s.WeightKg)
                .HasPrecision(7, 2);

            entity.Property(s => s.Rpe)
                .HasPrecision(3, 1);

            entity.Property(s => s.Notes)
                .HasMaxLength(500);
        });*/

        modelBuilder.Entity<BodyMeasurement>(entity =>
        {
            entity.Property(b => b.UserId)
                .HasMaxLength(450);

            entity.HasIndex(b => b.UserId);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(b => b.WeightKg)
                .HasPrecision(7, 2);

            /*entity.Property(b => b.BodyFatPercentage)
                .HasPrecision(5, 2);*/

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
