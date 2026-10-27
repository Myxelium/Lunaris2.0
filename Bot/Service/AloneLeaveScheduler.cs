namespace Lunaris2.Service;

/// <summary>
/// One pending leave timer per voice channel. Completing an older timer must not cancel the timer that replaced it.
/// </summary>
public sealed class AloneLeaveScheduler
{
    private readonly object _gate = new();
    private readonly Dictionary<ulong, CancellationTokenSource> _pending = new();

    public bool TrySchedule(ulong channelId, out CancellationTokenSource? schedule)
    {
        lock (_gate)
        {
            if (_pending.ContainsKey(channelId))
            {
                schedule = null;
                return false;
            }

            schedule = new CancellationTokenSource();
            _pending[channelId] = schedule;
            return true;
        }
    }

    public void Cancel(ulong channelId)
    {
        lock (_gate)
        {
            if (!_pending.Remove(channelId, out var schedule))
                return;

            schedule.Cancel();
        }
    }

    public void CompleteIfCurrent(ulong channelId, CancellationTokenSource schedule)
    {
        lock (_gate)
        {
            if (_pending.TryGetValue(channelId, out var current) && ReferenceEquals(current, schedule))
                _pending.Remove(channelId);
        }

        schedule.Dispose();
    }

    public bool IsScheduled(ulong channelId)
    {
        lock (_gate)
            return _pending.ContainsKey(channelId);
    }

    public void CancelAll()
    {
        List<CancellationTokenSource> schedules;
        lock (_gate)
        {
            schedules = _pending.Values.ToList();
            _pending.Clear();
        }

        foreach (var schedule in schedules)
            schedule.Cancel();
    }
}
