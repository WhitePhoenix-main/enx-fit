using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public class BodyMeasurementService(ApplicationDbContext dbContext, CurrentUser currentUser)
{
    public async Task<IReadOnlyList<BodyMeasurement>> GetAllAsync(string? ownerId = null)
    {
        var query = VisibleMeasurements();

        if (currentUser.IsAdministrator && !string.IsNullOrWhiteSpace(ownerId))
        {
            query = query.Where(measurement => measurement.UserId == ownerId);
        }

        return await query
            .AsNoTracking()
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Id)
            .ToListAsync();
    }

    public Task<BodyMeasurement?> FindAsync(int id) =>
        VisibleMeasurements()
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == id);

    public async Task CreateAsync(BodyMeasurementInputModel input)
    {
        var measurement = new BodyMeasurement
        {
            UserId = currentUser.ResolveOwnerId(input.OwnerId)
        };
        ApplyInput(measurement, input);
        dbContext.BodyMeasurements.Add(measurement);
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(int id, BodyMeasurementInputModel input)
    {
        var measurement = await VisibleMeasurements().SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (measurement is null)
        {
            return false;
        }

        ApplyInput(measurement, input);
        if (currentUser.IsAdministrator && !string.IsNullOrWhiteSpace(input.OwnerId))
        {
            measurement.UserId = input.OwnerId;
        }
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var measurement = await VisibleMeasurements().SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (measurement is null)
        {
            return false;
        }

        dbContext.BodyMeasurements.Remove(measurement);
        await dbContext.SaveChangesAsync();
        return true;
    }

    private static void ApplyInput(BodyMeasurement measurement, BodyMeasurementInputModel input)
    {
        measurement.Date = input.Date;
        measurement.WeightKg = input.WeightKg;
        measurement.HeightCm = input.HeightCm;
        measurement.BodyFatPercent = input.BodyFatPercent;
        measurement.WaistCm = input.WaistCm;
        measurement.ChestCm = input.ChestCm;
        measurement.HipCm = input.HipCm;
        measurement.ArmCm = input.ArmCm;
        measurement.ThighCm = input.ThighCm;
        measurement.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
    }

    private IQueryable<BodyMeasurement> VisibleMeasurements() =>
        currentUser.IsAdministrator
            ? dbContext.BodyMeasurements
            : dbContext.BodyMeasurements.Where(measurement => measurement.UserId == currentUser.Id);
}
