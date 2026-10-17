using System.Diagnostics;
using System.Globalization;

namespace enx_fit.Diagnostics;

public sealed class RequestPerformance
{
    private readonly Dictionary<string, double> stages = new();
    public bool Enabled { get; set; }
    public int SqlCommands { get; private set; }
    public double SqlMilliseconds { get; private set; }
    public string Stages => string.Join(", ", stages.Select(stage =>
        $"{stage.Key}={stage.Value.ToString("F1", CultureInfo.InvariantCulture)}ms"));

    public async Task<T> MeasureAsync<T>(string name, Func<Task<T>> action)
    {
        using var measurement = Measure(name);
        return await action();
    }

    public IDisposable? Measure(string name) => Enabled ? new Measurement(this, name) : null;

    public void RecordSql(TimeSpan duration)
    {
        if (!Enabled) return;
        SqlCommands++;
        SqlMilliseconds += duration.TotalMilliseconds;
    }

    public string ServerTiming(double elapsedMilliseconds) => string.Join(", ",
        stages.Select(stage => $"{stage.Key};dur={Number(stage.Value)}")
            .Append($"db;dur={Number(SqlMilliseconds)};desc=\"{SqlCommands} SQL commands\"")
            .Append($"app;dur={Number(elapsedMilliseconds)}"));

    private static string Number(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    private sealed class Measurement(RequestPerformance owner, string name) : IDisposable
    {
        private readonly long started = Stopwatch.GetTimestamp();
        public void Dispose() => owner.stages[name] = owner.stages.GetValueOrDefault(name) +
            Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }
}
