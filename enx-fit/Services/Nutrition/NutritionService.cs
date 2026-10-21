using enx_fit.Data;
using enx_fit.Models;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Nutrition;

public sealed record NutritionDay(NutritionSettings Settings, IReadOnlyList<MealEntry> Meals, int WaterMl)
{
    public int Calories => Meals.Sum(m => m.Calories);
    public int Protein => Meals.Sum(m => m.Protein);
    public int Fat => Meals.Sum(m => m.Fat);
    public int Carbs => Meals.Sum(m => m.Carbs);
}
public sealed record NutritionHistoryDay(DateOnly Date, int Calories, int Protein, int Fat, int Carbs);

public sealed class NutritionService(ApplicationDbContext db)
{
    public async Task<IReadOnlyList<NutritionHistoryDay>> HistoryAsync(string userId, DateOnly until) => await db.MealEntries.AsNoTracking()
        .Where(m => m.UserId == userId && m.Date <= until && m.Date >= until.AddDays(-6)).GroupBy(m => m.Date)
        .OrderByDescending(g => g.Key).Select(g => new NutritionHistoryDay(g.Key, g.Sum(m => m.Calories), g.Sum(m => m.Protein), g.Sum(m => m.Fat), g.Sum(m => m.Carbs))).ToListAsync();
    public async Task<NutritionDay> LoadAsync(string userId, DateOnly date) => new(
        await db.Set<NutritionSettings>().AsNoTracking().SingleOrDefaultAsync(s => s.UserId == userId) ?? new() { UserId = userId },
        await db.Set<MealEntry>().AsNoTracking().Where(m => m.UserId == userId && m.Date == date).OrderBy(m => m.Time).ThenBy(m => m.Id).ToListAsync(),
        await db.DailyCheckIns.Where(d => d.UserId == userId && d.Date == date).Select(d => d.WaterMl).SingleOrDefaultAsync());

    public async Task SaveMealAsync(MealEntry entry)
    {
        db.Set<MealEntry>().Add(entry);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteMealAsync(string userId, int id, DateOnly date) =>
        await db.Set<MealEntry>().Where(m => m.UserId == userId && m.Id == id && m.Date == date).ExecuteDeleteAsync() == 1;

    public async Task SaveGoalsAsync(NutritionSettings settings)
    {
        var existing = await db.Set<NutritionSettings>().SingleOrDefaultAsync(s => s.UserId == settings.UserId);
        if (existing is null) db.Add(settings);
        else db.Entry(existing).CurrentValues.SetValues(settings);
        await db.SaveChangesAsync();
    }

    public async Task AddWaterAsync(string userId, DateOnly date)
    {
        var query = db.DailyCheckIns.Where(d => d.UserId == userId && d.Date == date);
        if (await query.ExecuteUpdateAsync(u => u.SetProperty(d => d.WaterMl, d => Math.Min(10000, d.WaterMl + 250))) > 0) return;
        var checkIn = new DailyCheckIn { UserId = userId, Date = date, WaterMl = 250 };
        db.DailyCheckIns.Add(checkIn);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            db.Entry(checkIn).State = EntityState.Detached;
            await query.ExecuteUpdateAsync(u => u.SetProperty(d => d.WaterMl, d => Math.Min(10000, d.WaterMl + 250)));
        }
    }
}
