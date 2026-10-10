using enx_fit.Models;

namespace enx_fit.Services;

public sealed record WorkoutExerciseOrderResult(WorkoutExecutionState State, IReadOnlyList<int> Order);

public partial class WorkoutService
{
    public async Task<WorkoutExerciseOrderResult> ReorderExercisesAsync(int id, IReadOnlyList<int> entryIds,
        Guid? operationId = null, Guid? expectedRevision = null)
    {
        if (await CommandExistsAsync(id, operationId))
        {
            var existing = await FindAsync(id) ?? throw new KeyNotFoundException();
            return new(State(existing), existing.WorkoutExercises.OrderBy(e => e.Order).ThenBy(e => e.Id).Select(e => e.Id).ToArray());
        }
        var session = await EditableSessionAsync(id);
        CheckRevision(session, expectedRevision);
        var ordered = session.WorkoutExercises.OrderBy(e => e.Order).ThenBy(e => e.Id).ToArray();
        if (entryIds.Count != ordered.Length || entryIds.Count > 40 || entryIds.Distinct().Count() != entryIds.Count ||
            !entryIds.Order().SequenceEqual(ordered.Select(e => e.Id).Order()))
            throw new InvalidOperationException("Состав тренировки изменился. Обновите страницу перед перестановкой.");
        var blocks = WorkoutExerciseBlocks.Describe(session);
        if (!entryIds.Select(entry => blocks[entry].Key).SequenceEqual(ordered.Select(e => blocks[e.Id].Key)))
            throw new InvalidOperationException("В занятии меняется порядок внутри блока. Перенос между блоками настраивается в конструкторе.");
        var byId = ordered.ToDictionary(e => e.Id);
        for (var i = 0; i < entryIds.Count; i++) byId[entryIds[i]].Order = i + 1;
        await SaveCommandAsync(session, operationId);
        return new(State(session), entryIds.ToArray());
    }
}
