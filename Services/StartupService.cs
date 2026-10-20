using Microsoft.Win32;

namespace SpotifyNowPlaying.Services;

public static class StartupService
{
    private const string AppName = "SpotifyNowPlaying";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Kiểm tra xem ứng dụng đã được cấu hình khởi động cùng Windows hay chưa
    /// </summary>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            var value = key?.GetValue(AppName) as string;
            if (string.IsNullOrEmpty(value)) return false;

            var currentExe = Environment.ProcessPath;
            return !string.IsNullOrEmpty(currentExe) && value.Contains(currentExe, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StartupService] Lỗi kiểm tra Startup: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Đăng ký hoặc hủy đăng ký khởi động cùng Windows
    /// </summary>
    public static bool SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return false;

            if (enable)
            {
                var currentExe = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(currentExe))
                {
                    // Thêm cờ --startup để ứng dụng biết là được khởi động cùng Windows và ngủ ngầm yên lặng
                    key.SetValue(AppName, $"\"{currentExe}\" --startup");
                    return true;
                }
                return false;
            }
            else
            {
                key.DeleteValue(AppName, false);
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StartupService] Lỗi thiết lập Startup: {ex.Message}");
            return false;
        }
    }
}
