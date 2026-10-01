using enx_fit.Models;

namespace enx_fit.ViewModels;

public sealed class ExerciseLibraryModel
{
    public required string Id { get; init; }
    public required string ViewerId { get; init; }
    public required IReadOnlyList<Exercise> Exercises { get; init; }
    public IReadOnlyCollection<int> SelectedIds { get; init; } = [];
    public IReadOnlyCollection<int> RecentIds { get; init; } = [];
    public bool ForBuilder { get; init; }
}
