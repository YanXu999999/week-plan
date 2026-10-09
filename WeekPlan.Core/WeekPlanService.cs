namespace WeekPlan;

public sealed class WeekPlanService
{
    private readonly AppDatabase _database;

    public WeekPlanService(AppDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<TaskItem>> GetDayAsync(int dayOfWeek, long epochDay)
    {
        var tasks = await _database.Connection.Table<TaskEntity>()
            .Where(task => task.DayOfWeek == dayOfWeek)
            .ToListAsync();
        tasks = tasks.OrderBy(task => task.SortOrder).ThenBy(task => task.Id).ToList();
        var logs = await _database.Connection.Table<TaskLogEntity>()
            .Where(log => log.EpochDay == epochDay)
            .ToListAsync();
        var byTask = logs.ToDictionary(log => log.TaskId);
        return tasks.Select(task => ToItem(task, byTask.GetValueOrDefault(task.Id))).ToList();
    }

    public async Task<SessionItem?> GetSessionAsync(long taskId, long epochDay)
    {
        var task = await _database.Connection.FindAsync<TaskEntity>(taskId);
        if (task is null || !PlanCodes.IsTraining(task.Type))
            return null;

        var log = await FindLogAsync(taskId, epochDay);
        if (PlanCodes.IsCardio(task.Type))
        {
            var cardio = ToCardio(task, log);
            return new SessionItem(
                task.Id,
                task.Title,
                task.Sets,
                task.RestSeconds,
                cardio.Status,
                1,
                cardio.EndsAt,
                PlanCodes.Cardio,
                cardio.DurationMinutes);
        }

        var snapshot = ToSnapshot(task, log);
        return new SessionItem(
            task.Id,
            task.Title,
            snapshot.Sets,
            snapshot.RestSeconds,
            snapshot.Status,
            snapshot.CurrentSet,
            snapshot.RestEndsAt,
            PlanCodes.Strength,
            task.DurationMinutes);
    }

    public async Task<EditorItem?> GetEditorAsync(long? taskId, int dayOfWeek, long epochDay)
    {
        if (taskId is null)
        {
            return new EditorItem(
                null,
                dayOfWeek,
                epochDay,
                true,
                "",
                PlanCodes.Strength,
                PlanLimits.DefaultSets,
                PlanLimits.DefaultRestSeconds,
                PlanCodes.NotStarted,
                PlanLimits.DefaultDurationMinutes);
        }

        var task = await _database.Connection.FindAsync<TaskEntity>(taskId.Value);
        if (task is null)
            return null;

        var log = await FindLogAsync(task.Id, epochDay);
        if (PlanCodes.IsCardio(task.Type))
        {
            var cardio = ToCardio(task, log);
            return new EditorItem(
                task.Id,
                task.DayOfWeek,
                epochDay,
                false,
                task.Title,
                PlanCodes.Cardio,
                task.Sets,
                task.RestSeconds,
                cardio.Status,
                cardio.DurationMinutes);
        }

        var snapshot = ToSnapshot(task, log);
        return new EditorItem(
            task.Id,
            task.DayOfWeek,
            epochDay,
            false,
            task.Title,
            PlanCodes.Canonical(task.Type),
            snapshot.Sets,
            snapshot.RestSeconds,
            snapshot.Status,
            task.DurationMinutes);
    }

    public async Task<bool> SaveAsync(TaskDraft draft)
    {
        var title = draft.Title.Trim();
        if (title.Length == 0)
            return false;

        var type = PlanCodes.Canonical(draft.Type);
        var sets = Math.Clamp(draft.Sets, PlanLimits.MinSets, PlanLimits.MaxSets);
        var rest = Math.Clamp(draft.RestSeconds, PlanLimits.MinRestSeconds, PlanLimits.MaxRestSeconds);
        var duration = CardioSessionRules.Clamp(draft.DurationMinutes);

        if (draft.TaskId is null)
        {
            var existing = await _database.Connection.Table<TaskEntity>()
                .Where(task => task.DayOfWeek == draft.DayOfWeek)
                .ToListAsync();
            var sort = existing.Count == 0 ? 1 : existing.Max(task => task.SortOrder) + 1;
            await _database.Connection.InsertAsync(new TaskEntity
            {
                DayOfWeek = draft.DayOfWeek,
                Type = type,
                Title = title,
                Sets = type == PlanCodes.Strength ? sets : PlanLimits.DefaultSets,
                RestSeconds = type == PlanCodes.Strength ? rest : PlanLimits.DefaultRestSeconds,
                DurationMinutes = type == PlanCodes.Cardio ? duration : PlanLimits.DefaultDurationMinutes,
                SortOrder = sort,
            });
            return true;
        }

        var current = await _database.Connection.FindAsync<TaskEntity>(draft.TaskId.Value);
        if (current is null)
            return false;

        current.Title = title;
        if (PlanCodes.IsStrength(current.Type))
        {
            current.Sets = sets;
            current.RestSeconds = rest;
        }
        else if (PlanCodes.IsCardio(current.Type))
        {
            current.DurationMinutes = duration;
        }

        await _database.Connection.UpdateAsync(current);
        if (PlanCodes.IsTraining(current.Type) && draft.Status is not null)
            await SetFitnessStatusAsync(current.Id, draft.EpochDay, draft.Status, AppClock.NowMillis());
        if (PlanCodes.IsStrength(current.Type))
            await ChangeSetsAsync(current.Id, draft.EpochDay, sets);
        return true;
    }

    public async Task DeleteAsync(long taskId)
    {
        var logs = await _database.Connection.Table<TaskLogEntity>()
            .Where(log => log.TaskId == taskId)
            .ToListAsync();
        foreach (var log in logs)
            await _database.Connection.DeleteAsync(log);

        var task = await _database.Connection.FindAsync<TaskEntity>(taskId);
        if (task is not null)
            await _database.Connection.DeleteAsync(task);
    }

    public async Task SetFitnessStatusAsync(long taskId, long epochDay, string status, long now)
    {
        var task = await _database.Connection.FindAsync<TaskEntity>(taskId);
        if (task is null || !PlanCodes.IsTraining(task.Type))
            return;

        var log = await GetOrCreateLogAsync(task, epochDay);
        if (PlanCodes.IsCardio(task.Type))
            ApplyCardio(log, CardioSessionRules.SetStatus(ToCardio(task, log), status, now));
        else
            Apply(log, FitnessSessionRules.SetStatus(ToSnapshot(task, log), status));
        await _database.Connection.UpdateAsync(log);
    }

    public async Task CompleteSetAsync(long taskId, long epochDay, long now)
    {
        var task = await _database.Connection.FindAsync<TaskEntity>(taskId);
        if (task is null || !PlanCodes.IsStrength(task.Type))
            return;

        var log = await FindLogAsync(taskId, epochDay);
        if (log is null)
            return;

        var next = FitnessSessionRules.CompleteSet(ToSnapshot(task, log), now);
        Apply(log, next);
        await _database.Connection.UpdateAsync(log);
    }

    public async Task ChangeSetsAsync(long taskId, long epochDay, int sets)
    {
        var task = await _database.Connection.FindAsync<TaskEntity>(taskId);
        if (task is null || !PlanCodes.IsStrength(task.Type))
            return;

        var log = await FindLogAsync(taskId, epochDay);
        var snapshot = ToSnapshot(task, log);
        snapshot = FitnessSessionRules.SyncRest(snapshot, AppClock.NowMillis());
        snapshot = FitnessSessionRules.ChangeSets(snapshot, sets);
        task.Sets = snapshot.Sets;
        await _database.Connection.UpdateAsync(task);
        if (log is null)
            return;

        Apply(log, snapshot);
        await _database.Connection.UpdateAsync(log);
    }

    public async Task ChangeRestAsync(long taskId, int restSeconds)
    {
        var task = await _database.Connection.FindAsync<TaskEntity>(taskId);
        if (task is null || !PlanCodes.IsStrength(task.Type))
            return;

        task.RestSeconds = Math.Clamp(restSeconds, PlanLimits.MinRestSeconds, PlanLimits.MaxRestSeconds);
        await _database.Connection.UpdateAsync(task);
    }

    public async Task SyncRestsAsync(long epochDay, long now)
    {
        var logs = await _database.Connection.QueryAsync<TaskLogEntity>(
            "SELECT * FROM task_logs WHERE EpochDay = ? AND Status = ? AND RestEndsAt IS NOT NULL",
            epochDay,
            PlanCodes.InProgress);
        foreach (var log in logs)
        {
            if (log.RestEndsAt is long restEnd && now < restEnd)
                continue;

            var task = await _database.Connection.FindAsync<TaskEntity>(log.TaskId);
            if (task is null)
                continue;

            if (PlanCodes.IsCardio(task.Type))
            {
                var current = ToCardio(task, log);
                var next = CardioSessionRules.Sync(current, now);
                if (next == current)
                    continue;

                ApplyCardio(log, next);
            }
            else if (PlanCodes.IsStrength(task.Type))
            {
                var current = ToSnapshot(task, log);
                var next = FitnessSessionRules.SyncRest(current, now);
                if (next == current)
                    continue;

                Apply(log, next);
            }
            else
            {
                continue;
            }

            await _database.Connection.UpdateAsync(log);
        }
    }

    private async Task<TaskLogEntity?> FindLogAsync(long taskId, long epochDay)
    {
        var logs = await _database.Connection.Table<TaskLogEntity>()
            .Where(log => log.TaskId == taskId && log.EpochDay == epochDay)
            .ToListAsync();
        return logs.FirstOrDefault();
    }

    private async Task<TaskLogEntity> GetOrCreateLogAsync(TaskEntity task, long epochDay)
    {
        var existing = await FindLogAsync(task.Id, epochDay);
        if (existing is not null)
            return existing;

        var created = new TaskLogEntity
        {
            TaskId = task.Id,
            EpochDay = epochDay,
            Status = PlanCodes.NotStarted,
            CurrentSet = 1,
            RestEndsAt = null,
        };
        await _database.Connection.InsertAsync(created);
        return created;
    }

    private static TaskItem ToItem(TaskEntity task, TaskLogEntity? log)
    {
        if (PlanCodes.IsCardio(task.Type))
        {
            var cardio = ToCardio(task, log);
            return new TaskItem(
                task.Id,
                task.DayOfWeek,
                PlanCodes.Cardio,
                task.Title,
                task.Sets,
                task.RestSeconds,
                cardio.Status,
                1,
                cardio.EndsAt,
                cardio.DurationMinutes);
        }

        var snapshot = ToSnapshot(task, log);
        return new TaskItem(
            task.Id,
            task.DayOfWeek,
            PlanCodes.Canonical(task.Type),
            task.Title,
            snapshot.Sets,
            snapshot.RestSeconds,
            snapshot.Status,
            snapshot.CurrentSet,
            snapshot.RestEndsAt,
            task.DurationMinutes);
    }

    private static CardioSnapshot ToCardio(TaskEntity task, TaskLogEntity? log)
    {
        var minutes = CardioSessionRules.Clamp(task.DurationMinutes);
        return log is null
            ? CardioSessionRules.Fresh(minutes)
            : new CardioSnapshot(log.Status, log.RestEndsAt, minutes);
    }

    private static void ApplyCardio(TaskLogEntity log, CardioSnapshot snapshot)
    {
        log.Status = snapshot.Status;
        log.CurrentSet = 1;
        log.RestEndsAt = snapshot.EndsAt;
    }

    private static SessionSnapshot ToSnapshot(TaskEntity task, TaskLogEntity? log) =>
        log is null
            ? FitnessSessionRules.Fresh(task.Sets, task.RestSeconds)
            : new SessionSnapshot(log.Status, log.CurrentSet, log.RestEndsAt, task.Sets, task.RestSeconds);

    private static void Apply(TaskLogEntity log, SessionSnapshot snapshot)
    {
        log.Status = snapshot.Status;
        log.CurrentSet = snapshot.CurrentSet;
        log.RestEndsAt = snapshot.RestEndsAt;
    }
}
