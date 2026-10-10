using enx_fit.Models;

namespace enx_fit.ViewModels;

public sealed class ExerciseLibraryModel
{
    public required string Id { get; init; }
    public required string ViewerId { get; init; }
    public required IReadOnlyList<Exercise> Exercises { get; init; }
    public IReadOnlyCollection<Guid> SelectedIds { get; init; } = [];
    public IReadOnlyCollection<Guid> RecentIds { get; init; } = [];
    public bool ForBuilder { get; init; }
    public bool ForSelection { get; init; }
    public string ContextName { get; init; } = "Текущая тренировка";
}
