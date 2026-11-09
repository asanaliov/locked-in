using System.Net.Sockets;

namespace LockedIn.Linux;

/// <summary>
/// Only one Locked In may run, or every second would be recorded twice. A locked file decides who's first, and the
/// kernel releases it even after a crash. Starting it again connects to a socket that brings the window to the front.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private static readonly string Folder = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 } runtime
        ? runtime
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), "LockedIn");

    private static string LockPath => Path.Combine(Folder, "locked-in.lock");
    private static string SocketPath => Path.Combine(Folder, "locked-in.sock");

    private readonly FileStream _lock;
    private readonly Socket _listener;
    private readonly CancellationTokenSource _stop = new();

    private SingleInstance(FileStream fileLock)
    {
        _lock = fileLock;

        // We hold the lock, so any socket file left here belongs to a run that has ended.
        File.Delete(SocketPath);
        _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listener.Bind(new UnixDomainSocketEndPoint(SocketPath));
        _listener.Listen();
        _ = ListenAsync();
    }

    /// <summary>Raised on a background thread when another launch asks for the window.</summary>
    public event Action? ActivateRequested;

    public static SingleInstance? TryAcquire()
    {
        Directory.CreateDirectory(Folder);
        try
        {
            return new SingleInstance(new FileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void ActivateRunningInstance()
    {
        try
        {
            using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            client.Connect(new UnixDomainSocketEndPoint(SocketPath));
        }
        catch (SocketException)
        {
            // The other instance is just exiting; there is no window to show.
        }
    }

    private async Task ListenAsync()
    {
        try
        {
            while (true)
            {
                using var client = await _listener.AcceptAsync(_stop.Token);
                ActivateRequested?.Invoke();
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
        {
            // Shutting down.
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Dispose();
        File.Delete(SocketPath);
        _lock.Dispose();
        _stop.Dispose();
    }
}
