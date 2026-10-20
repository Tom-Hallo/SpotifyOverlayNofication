using System.IO;
using System.Text.Json;
using SpotifyNowPlaying.Models;

namespace SpotifyNowPlaying.Services;

public class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    public AppConfig CurrentConfig { get; private set; } = new();

    public event Action<AppConfig>? ConfigChanged;

    public void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                CurrentConfig = JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
            }
            else
            {
                // Nếu chưa có file config, tạo file mặc định
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

    public void SaveConfig(AppConfig config)
    {
        try
        {
            CurrentConfig = config;
            var json = JsonSerializer.Serialize(CurrentConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
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
        }
        catch { }
    }
}
