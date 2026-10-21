using System.Text.Json.Serialization;

namespace SpotifyNowPlaying.Models;

public class AppConfig
{
    public PositionConfig Position { get; set; } = new();
    public int DisplayDurationMs { get; set; } = 4000;
    public int AnimationDurationMs { get; set; } = 300;
    public StyleConfig Style { get; set; } = new();
    public AutoLifeCycleConfig AutoLifeCycle { get; set; } = new();
}

public class AutoLifeCycleConfig
{
    // Tự động kích hoạt khi mở Spotify và ngủ ngầm khi tắt Spotify
    public bool AutoSyncWithSpotify { get; set; } = true;

    // Tự động đăng ký khởi động cùng Windows
    public bool StartWithWindows { get; set; } = true;

    // Khoảng thời gian kiểm tra tiến trình Spotify (ms)
    public int CheckIntervalMs { get; set; } = 1500;
}

public class PositionConfig
{
    // Hỗ trợ các kiểu canh lề: TopRight, TopCenter, TopLeft, BottomRight, BottomLeft, Custom
    public string Alignment { get; set; } = "TopRight";
    public double OffsetX { get; set; } = 0;
    public double OffsetY { get; set; } = 20;
    public double CustomX { get; set; } = 0;
    public double CustomY { get; set; } = 0;
}

public class StyleConfig
{
    public double Width { get; set; } = 410;
    public double Height { get; set; } = 100;
    
    // Bo góc riêng biệt cho 2 góc bên Trái và 2 góc bên Phải
    public double? CornerRadiusLeft { get; set; }
    public double? CornerRadiusRight { get; set; }
    public double? CornerRadius { get; set; }

    public string BackgroundColor { get; set; } = "#E6181818";
    public string TextColor { get; set; } = "#FFFFFF";
    public string AccentColor { get; set; } = "#1DB954";

    public double GetLeftRadius()
    {
        if (CornerRadiusLeft.HasValue) return CornerRadiusLeft.Value;
        if (CornerRadius.HasValue) return CornerRadius.Value;
        return 24;
    }

    public double GetRightRadius()
    {
        if (CornerRadiusRight.HasValue) return CornerRadiusRight.Value;
        return 0;
    }
}
