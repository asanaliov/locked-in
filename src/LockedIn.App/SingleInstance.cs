namespace LockedIn.App;

/// <summary>
/// Only one locked-in may run, or every second would be recorded twice.
/// Starting it again just brings the running one's window to the front.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    /// <summary>Also used by the installer to detect a running copy.</summary>
    private const string MutexName = "LockedIn.App";
    private const string ActivateEventName = "LockedIn.App.Activate";

    private readonly Mutex _mutex;

    private SingleInstance(Mutex mutex)
    {
        _mutex = mutex;
        ActivateRequested = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
    }

    /// <summary>Signalled when another launch asks this instance to show its window.</summary>
    public EventWaitHandle ActivateRequested { get; }

    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirst);
        if (isFirst)
            return new SingleInstance(mutex);

        mutex.Dispose();
        return null;
    }

    public static void ActivateRunningInstance()
    {
        if (EventWaitHandle.TryOpenExisting(ActivateEventName, out var activate))
        {
            using (activate)
                activate.Set();
        }
    }

    public void Dispose()
    {
        ActivateRequested.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
