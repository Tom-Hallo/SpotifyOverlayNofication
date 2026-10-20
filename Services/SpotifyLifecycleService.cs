using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace SpotifyNowPlaying.Services;

public class SpotifyLifecycleService : IDisposable
{
    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

    private readonly DispatcherTimer _pollTimer;
    private bool _isSpotifyRunning;
    private bool _disposed;

    public event Action? SpotifyStarted;
    public event Action? SpotifyExited;

    public bool IsSpotifyRunning => _isSpotifyRunning;

    public SpotifyLifecycleService(int intervalMs = 1500)
    {
        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(500, intervalMs))
        };
        _pollTimer.Tick += (s, e) => CheckSpotifyStatus();
    }

    public void Start()
    {
        // Kiểm tra ngay lập tức khi khởi động
        _isSpotifyRunning = CheckIsSpotifyRunning();
        _pollTimer.Start();
    }

    public void Stop()
    {
        _pollTimer.Stop();
    }

    public void UpdateInterval(int intervalMs)
    {
        _pollTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(500, intervalMs));
    }

    /// <summary>
    /// Kiểm tra trạng thái tiến trình Spotify và kích hoạt event tương ứng nếu trạng thái thay đổi
    /// </summary>
    public void CheckSpotifyStatus()
    {
        if (_disposed) return;

        bool running = CheckIsSpotifyRunning();

        if (running && !_isSpotifyRunning)
        {
            _isSpotifyRunning = true;
            SpotifyStarted?.Invoke();
        }
        else if (!running && _isSpotifyRunning)
        {
            _isSpotifyRunning = false;
            SpotifyExited?.Invoke();
        }
    }

    /// <summary>
    /// Kiểm tra xem có bất kỳ tiến trình nào tên "spotify" đang chạy không
    /// </summary>
    public static bool CheckIsSpotifyRunning()
    {
        try
        {
            return Process.GetProcessesByName("spotify").Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Thu hồi bộ nhớ RAM và giải phóng working set khi vào trạng thái ngủ ngầm
    /// </summary>
    public static void TrimMemory()
    {
        try
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pollTimer.Stop();
    }
}
