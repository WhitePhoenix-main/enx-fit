using enx_fit.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace enx_fit.Security;

public enum SubscriptionPlan { Free, Pro, Coach }

public enum Feature
{
    ProgramBasic, ProgramUnlimited, ProgramAutoProgression, ProgramAdvancedPeriodization,
    ProgramAdvancedAnalytics, CoachClientManagement, CoachProgramAssignment, CoachClientAlerts,
    CoachReports, AnalyticsAdvanced, AnalyticsAIInsights,
    ProgramProgressionManual, ProgressionRecommendations, AnalyticsPlateauDetection,
    AnalyticsTrainingVolume, AnalyticsMuscleBalance, AnalyticsProgramEffectiveness,
    RecoveryDeloadRecommendations, CoachAnalytics
}

public sealed class FeatureOptions
{
    public Dictionary<SubscriptionPlan, Feature[]> Plans { get; set; } = new()
    {
        [SubscriptionPlan.Free] = [Feature.ProgramBasic, Feature.ProgramUnlimited, Feature.ProgramProgressionManual],
        [SubscriptionPlan.Pro] = [Feature.ProgramBasic, Feature.ProgramUnlimited, Feature.ProgramAutoProgression,
            Feature.ProgramProgressionManual, Feature.ProgressionRecommendations, Feature.AnalyticsPlateauDetection,
            Feature.ProgramAdvancedPeriodization, Feature.ProgramAdvancedAnalytics, Feature.AnalyticsAdvanced,
            Feature.AnalyticsTrainingVolume, Feature.AnalyticsMuscleBalance, Feature.AnalyticsProgramEffectiveness,
            Feature.RecoveryDeloadRecommendations],
        [SubscriptionPlan.Coach] = [Feature.ProgramBasic, Feature.ProgramUnlimited, Feature.ProgramAutoProgression,
            Feature.ProgramAdvancedPeriodization, Feature.ProgramAdvancedAnalytics, Feature.AnalyticsAdvanced,
            Feature.ProgramProgressionManual, Feature.ProgressionRecommendations, Feature.AnalyticsPlateauDetection,
            Feature.AnalyticsTrainingVolume, Feature.AnalyticsMuscleBalance, Feature.AnalyticsProgramEffectiveness,
            Feature.RecoveryDeloadRecommendations, Feature.CoachAnalytics,
            Feature.CoachClientManagement, Feature.CoachProgramAssignment, Feature.CoachClientAlerts, Feature.CoachReports]
    };
}

public sealed record FeatureAccess(SubscriptionPlan Plan, UserRole Role, IReadOnlySet<Feature> Features)
{
    public bool IsCoach => Role == UserRole.Trainer || Role >= UserRole.Administrator;
    public bool CanUse(Feature feature) => Role >= UserRole.User && Features.Contains(feature) &&
        (feature is not (Feature.CoachClientManagement or Feature.CoachProgramAssignment or Feature.CoachClientAlerts or Feature.CoachReports or Feature.CoachAnalytics) || IsCoach);
}

public interface IFeatureAccessService
{
    Task<FeatureAccess> GetAsync();
    Task<bool> CanUseAsync(Feature feature);
}

public sealed class FeatureAccessService(ApplicationDbContext db, CurrentUser currentUser, IOptions<FeatureOptions> options) : IFeatureAccessService
{
    private FeatureAccess? cached;
    public async Task<FeatureAccess> GetAsync()
    {
        if (cached is not null) return cached;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == currentUser.Id);
        var role = user?.Role ?? UserRole.Guest;
        var plan = user?.SubscriptionPlan ?? SubscriptionPlan.Free;
        var features = role >= UserRole.Administrator ? Enum.GetValues<Feature>() :
            options.Value.Plans.GetValueOrDefault(plan) ?? [];
        // The diary, templates and manual programming are never subscription quotas.
        var capabilities = features.ToHashSet();
        capabilities.UnionWith([Feature.ProgramBasic, Feature.ProgramUnlimited, Feature.ProgramProgressionManual]);
        return cached = new(plan, role, capabilities);
    }
    public async Task<bool> CanUseAsync(Feature feature) => (await GetAsync()).CanUse(feature);
}

public static class ProgramServiceRegistration
{
    public static IServiceCollection AddTrainingPrograms(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FeatureOptions>().Bind(configuration.GetSection("ProgramFeatures"));
        services.AddScoped<IFeatureAccessService, FeatureAccessService>();
        services.AddScoped<Services.TrainingProgramService>();
        services.AddScoped<Services.ProgramTrainingDataService>();
        services.AddSingleton<Services.IProgressionService, Services.ProgressionService>();
        services.AddSingleton<Services.IPlateauDetectionService, Services.PlateauDetectionService>();
        services.AddScoped<Services.IRecommendationService, Services.RecommendationService>();
        services.AddSingleton<Services.IProgramAnalysisService, Services.ProgramAnalysisService>();
        services.AddSingleton<Services.ITrainingVolumeService, Services.TrainingVolumeService>();
        services.AddScoped<Services.CoachAttentionService>();
        return services;
    }
}
