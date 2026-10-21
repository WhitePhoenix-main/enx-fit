using enx_fit.Areas.Identity.Data;
using enx_fit.Models;
using enx_fit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();
    public DbSet<NutritionSettings> NutritionSettings => Set<NutritionSettings>();
    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
    public DbSet<ProgramScheduleChange> ProgramScheduleChanges => Set<ProgramScheduleChange>();
    public DbSet<AssignedProgram> AssignedPrograms => Set<AssignedProgram>();
    public DbSet<TrainingRecommendation> TrainingRecommendations => Set<TrainingRecommendation>();

    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();

    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();

    public DbSet<SetEntry> SetEntries => Set<SetEntry>();

    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<ProgressPhoto> ProgressPhotos => Set<ProgressPhoto>();
    public DbSet<DashboardSettings> DashboardSettings => Set<DashboardSettings>();
    public DbSet<DailyCheckIn> DailyCheckIns => Set<DailyCheckIn>();
    public DbSet<DashboardLayoutPreference> DashboardLayoutPreferences => Set<DashboardLayoutPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Keep Identity's key lengths identical in runtime and the standalone migration factory.
        modelBuilder.Entity<IdentityUserLogin<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        modelBuilder.Entity<IdentityUserLogin<string>>().Property(x => x.ProviderKey).HasMaxLength(128);
        modelBuilder.Entity<IdentityUserToken<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        modelBuilder.Entity<IdentityUserToken<string>>().Property(x => x.Name).HasMaxLength(128);
        modelBuilder.ConfigureTrainingPrograms();
        modelBuilder.Entity<WorkoutTemplateCollection>(entity =>
        {
            entity.Property(c => c.UserId).HasMaxLength(450);
            entity.Property(c => c.Name).HasMaxLength(60);
            entity.HasIndex(c => c.UserId);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CoachMessage>(entity =>
        {
            entity.Property(m => m.SenderId).HasMaxLength(450);
            entity.Property(m => m.RecipientId).HasMaxLength(450);
            entity.Property(m => m.Text).HasMaxLength(2000);
            entity.HasIndex(m => new { m.SenderId, m.RequestId }).IsUnique();
            entity.HasIndex(m => new { m.RecipientId, m.CreatedAtUtc });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(m => m.RecipientId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<NutritionSettings>(entity =>
        {
            entity.HasKey(s => s.UserId);
            entity.Property(s => s.Goal).HasMaxLength(120);
            entity.HasOne<ApplicationUser>().WithOne().HasForeignKey<NutritionSettings>(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MealEntry>(entity =>
        {
            entity.Property(m => m.UserId).HasMaxLength(450);
            entity.Property(m => m.Name).HasMaxLength(120);
            entity.Property(m => m.Type).HasMaxLength(20);
            entity.HasIndex(m => new { m.UserId, m.Date });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProgressPhoto>(entity =>
        {
            entity.Property(p => p.UserId).HasMaxLength(450);
            entity.Property(p => p.ContentType).HasMaxLength(20);
            entity.HasIndex(p => new { p.UserId, p.Date });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

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
            entity.Property(item => item.PreferencesRevision).IsConcurrencyToken();
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
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .IsRequired();

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.HasData(
                new Exercise { Id = ExerciseIds.FromLegacy(1), Name = "Bench Press", MuscleGroup = "Грудь", Equipment = "Штанга, скамья" },
                new Exercise { Id = ExerciseIds.FromLegacy(2), Name = "Squat", MuscleGroup = "Квадрицепсы", Equipment = "Штанга" },
                new Exercise { Id = ExerciseIds.FromLegacy(3), Name = "Deadlift", MuscleGroup = "Всё тело", Equipment = "Штанга" },
                new Exercise { Id = ExerciseIds.FromLegacy(4), Name = "Pull Up", MuscleGroup = "Спина", Equipment = "Турник" },
                new Exercise { Id = ExerciseIds.FromLegacy(5), Name = "Overhead Press", MuscleGroup = "Плечи", Equipment = "Штанга" },
                new Exercise { Id = ExerciseIds.FromLegacy(6), Name = "Romanian Deadlift", MuscleGroup = "Задняя поверхность бедра", Equipment = "Штанга" },
                new Exercise { Id = ExerciseIds.FromLegacy(7), Name = "Leg Press", MuscleGroup = "Квадрицепсы", Equipment = "Тренажёр для жима ногами" },
                new Exercise { Id = ExerciseIds.FromLegacy(8), Name = "Barbell Row", MuscleGroup = "Спина", Equipment = "Штанга" });
            entity.HasData(ExerciseCatalog.All);
        });

        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.Property(w => w.UserId)
                .HasMaxLength(450);

            entity.HasIndex(w => w.UserId);
            entity.HasIndex(w => new { w.UserId, w.ClientRequestId }).IsUnique();
            entity.Property(w => w.ManualPayloadHash).HasMaxLength(64);
            entity.HasIndex(w => w.UserId, "IX_WorkoutSessions_OneActivePerUser").IsUnique()
                .HasFilter("\"StartedAtUtc\" IS NOT NULL AND \"CompletedAtUtc\" IS NULL AND \"UserId\" IS NOT NULL");

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

        modelBuilder.Entity<ProgramScheduleChange>(entity =>
        {
            entity.HasIndex(c => new { c.TrainingProgramId, c.ProgramWorkoutKey, c.OriginalDate }).IsUnique();
            entity.HasOne<TrainingProgram>().WithMany(p => p.ScheduleChanges)
                .HasForeignKey(c => c.TrainingProgramId).OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<SetEntry>().Property(s => s.IsCompleted).HasDefaultValue(true).HasSentinel(true);
        modelBuilder.Entity<SetEntry>().OwnsMany(s => s.Drops, drop =>
        {
            drop.ToTable("SetDrops");
            drop.WithOwner().HasForeignKey("SetEntryId");
            drop.HasKey(d => d.Id);
            drop.Property(d => d.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<WorkoutCommand>(entity =>
        {
            entity.HasKey(c => new { c.WorkoutSessionId, c.OperationId });
            entity.HasOne(c => c.WorkoutSession).WithMany().HasForeignKey(c => c.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
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
