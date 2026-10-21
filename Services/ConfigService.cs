using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SpotifyNowPlaying.Models;

namespace SpotifyNowPlaying.Services;

public class ConfigService : IDisposable
{
    public static string ConfigPath { get; private set; } = ResolveConfigPath();
    public AppConfig CurrentConfig { get; private set; } = new();

    public event Action<AppConfig>? ConfigChanged;

    private FileSystemWatcher? _watcher;
    private DateTime _lastReadTime = DateTime.MinValue;
    private readonly object _lock = new();

    public ConfigService()
    {
        SetupFileWatcher();
    }

    /// <summary>
    /// Tự động tìm đường dẫn file config.json:
    /// Ưu tiên thư mục gốc chứa project (.csproj) nếu đang chạy trong môi trường phát triển (bin/Debug, bin/Release),
    /// giúp việc sửa config trực tiếp từ mã nguồn / VS Code có hiệu lực ngay lập tức.
    /// </summary>
    private static string ResolveConfigPath()
    {
        try
        {
            // 1. Kiểm tra từ Environment.CurrentDirectory (khi chạy dotnet run từ thư mục workspace)
            var currentDir = new DirectoryInfo(Environment.CurrentDirectory);
            while (currentDir != null)
            {
                var projectFile = Path.Combine(currentDir.FullName, "SpotifyNowPlaying.csproj");
                var sourceConfig = Path.Combine(currentDir.FullName, "config.json");
                if (File.Exists(projectFile) && File.Exists(sourceConfig))
                {
                    return sourceConfig;
                }

                currentDir = currentDir.Parent;
            }

            // 2. Tìm ngược lên từ AppDomain.CurrentDomain.BaseDirectory (bin/Debug hoặc bin/Release)
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var projectFile = Path.Combine(dir.FullName, "SpotifyNowPlaying.csproj");
                var sourceConfig = Path.Combine(dir.FullName, "config.json");
                if (File.Exists(projectFile) && File.Exists(sourceConfig))
                {
                    return sourceConfig;
                }

                dir = dir.Parent;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigService] ResolveConfigPath error: {ex.Message}");
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    }

    private void SetupFileWatcher()
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            var filename = Path.GetFileName(ConfigPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;

            _watcher?.Dispose();
            _watcher = new FileSystemWatcher(directory, filename)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Renamed += (s, e) => OnFileChanged(s, e);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigService] Lỗi tạo FileSystemWatcher: {ex.Message}");
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            if ((DateTime.UtcNow - _lastReadTime).TotalMilliseconds < 500) return;
            _lastReadTime = DateTime.UtcNow;
        }

        // Chờ 150ms để trình soạn thảo giải phóng lock file
        Task.Delay(150).ContinueWith(_ =>
        {
            LoadConfig();
        });
    }

    public void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                // Mở với FileShare.ReadWrite để tránh xung đột lock khi đang sửa trong VS Code
                string json;
                using (var fs = new FileStream(ConfigPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    json = reader.ReadToEnd();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                CurrentConfig = JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();

                // Đồng bộ nội dung mới nhất sang thư mục bin
                SyncToBinDirectory(json);
            }
            else
            {
                CurrentConfig = new AppConfig();
                SaveDefaultConfig();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigService] Error loading config: {ex.Message}");
            CurrentConfig = new AppConfig();
        }

        ConfigChanged?.Invoke(CurrentConfig);
    }

    private static void SyncToBinDirectory(string json)
    {
        try
        {
            var binConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (!string.Equals(binConfig, ConfigPath, StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(binConfig, json);
            }
        }
        catch { }
    }

    public void SaveConfig(AppConfig config)
    {
        try
        {
            CurrentConfig = config;
            var json = JsonSerializer.Serialize(CurrentConfig, new JsonSerializerOptions { WriteIndented = true });

            if (_watcher != null) _watcher.EnableRaisingEvents = false;

            File.WriteAllText(ConfigPath, json);
            SyncToBinDirectory(json);

            if (_watcher != null) _watcher.EnableRaisingEvents = true;

            ConfigChanged?.Invoke(CurrentConfig);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigService] Error saving config: {ex.Message}");
        }
    }

    private void SaveDefaultConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(CurrentConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            SyncToBinDirectory(json);
        }
        catch { }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
