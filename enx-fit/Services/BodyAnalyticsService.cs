using enx_fit.Models;

namespace enx_fit.Services;

public class BodyAnalyticsService
{
    private const decimal StableWeightThresholdKg = 0.5m;

    public decimal? CalculateBmi(decimal weightKg, decimal heightCm)
    {
        if (weightKg <= 0 || heightCm <= 0)
        {
            return null;
        }

        var heightMeters = heightCm / 100;
        return Math.Round(weightKg / (heightMeters * heightMeters), 1);
    }

    public IReadOnlyList<BodyWeightPoint> GetWeightTrend(IEnumerable<BodyMeasurement> measurements) =>
        GetValidMeasurements(measurements)
            .Select(measurement => new BodyWeightPoint(
                measurement.Id,
                measurement.Date,
                measurement.WeightKg))
            .OrderBy(point => point.Date)
            .ThenBy(point => point.BodyMeasurementId)
            .ToList();

    public decimal? GetWeightChange(IEnumerable<BodyMeasurement> measurements)
    {
        var trend = GetWeightTrend(measurements);

        if (trend.Count < 2)
        {
            return null;
        }

        return trend[^1].WeightKg - trend[0].WeightKg;
    }

    public WeightTrendInterpretation GetWeightTrendInterpretation(IEnumerable<BodyMeasurement> measurements)
    {
        var change = GetWeightChange(measurements);

        if (!change.HasValue || Math.Abs(change.Value) <= StableWeightThresholdKg)
        {
            return WeightTrendInterpretation.Stable;
        }

        return change.Value > 0
            ? WeightTrendInterpretation.Increasing
            : WeightTrendInterpretation.Decreasing;
    }

    public string GetWeightTrendText(IEnumerable<BodyMeasurement> measurements) =>
        GetWeightTrendInterpretation(measurements) switch
        {
            WeightTrendInterpretation.Increasing => "Weight is increasing.",
            WeightTrendInterpretation.Decreasing => "Weight is decreasing.",
            _ => "Weight is stable."
        };

    private static IEnumerable<BodyMeasurement> GetValidMeasurements(IEnumerable<BodyMeasurement> measurements) =>
        measurements.Where(measurement =>
            measurement.WeightKg > 0);
}

public record BodyWeightPoint(
    int BodyMeasurementId,
    DateOnly Date,
    decimal WeightKg);

public enum WeightTrendInterpretation
{
    Stable,
    Increasing,
    Decreasing
}
