using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using SpotifyNowPlaying.Models;
using SpotifyNowPlaying.Services;

namespace SpotifyNowPlaying;

public partial class MainWindow : Window
{
    private readonly SpotifyMediaService _mediaService;
    private AppConfig _config;
    private readonly DispatcherTimer _hideTimer;
    private bool _isHiding = false;

    #region Win32 API: Đảm bảo không cướp Focus (WS_EX_NOACTIVATE & WS_EX_TOOLWINDOW)
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    #endregion

    public MainWindow(SpotifyMediaService mediaService, AppConfig initialConfig)
    {
        InitializeComponent();
        _mediaService = mediaService;
        _config = initialConfig;

        ApplyStyling();

        // Timer đếm ngược tự động ẩn flyout
        _hideTimer = new DispatcherTimer();
        _hideTimer.Tick += (s, e) =>
        {
            _hideTimer.Stop();
            HideWithAnimation();
        };

        // Khi di chuột vào flyout -> tạm dừng timer để người dùng kịp đọc hoặc bấm Skip
        MouseEnter += (s, e) => _hideTimer.Stop();
        MouseLeave += (s, e) =>
        {
            if (IsVisible && !_isHiding)
            {
                _hideTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1500, _config.DisplayDurationMs / 2));
                _hideTimer.Start();
            }
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Đảm bảo cửa sổ không bao giờ cướp focus của ứng dụng hoặc game người dùng đang thao tác
        var helper = new WindowInteropHelper(this);
        var exStyle = (int)GetWindowLong(helper.Handle, GWL_EXSTYLE);
        SetWindowLong(helper.Handle, GWL_EXSTYLE, (IntPtr)(exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW));
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        ApplyStyling();
    }

    private void ApplyStyling()
    {
        try
        {
            Width = _config.Style.Width;
            Height = _config.Style.Height;

            // Bo góc
            CardBorder.CornerRadius = new CornerRadius(_config.Style.CornerRadius);

            // Màu nền Card
            if (TryParseBrush(_config.Style.BackgroundColor, out var bgBrush))
            {
                CardBorder.Background = bgBrush;
            }

            // Màu chữ
            if (TryParseBrush(_config.Style.TextColor, out var textBrush))
            {
                TxtTitle.Foreground = textBrush;
                TxtArtist.Foreground = new SolidColorBrush(Color.FromArgb(180, textBrush.Color.R, textBrush.Color.G, textBrush.Color.B));
            }

            // Màu Accent (Spotify Green)
            if (TryParseBrush(_config.Style.AccentColor, out var accentBrush))
            {
                AccentBar.Background = accentBrush;
                SpotifyIcon.Fill = accentBrush;
                TxtAppLabel.Foreground = accentBrush;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Lỗi áp dụng Style: {ex.Message}");
        }
    }

    private static bool TryParseBrush(string hex, out SolidColorBrush brush)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            brush = new SolidColorBrush(color);
            return true;
        }
        catch
        {
            brush = new SolidColorBrush(Colors.White);
            return false;
        }
    }

    public void ShowTrack(TrackInfo track)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ShowTrack(track));
            return;
        }

        // Cập nhật thông tin bài hát
        TxtTitle.Text = string.IsNullOrWhiteSpace(track.Title) ? "Unknown Title" : track.Title;

        if (!string.IsNullOrWhiteSpace(track.AlbumTitle))
        {
            TxtArtist.Text = $"{track.Artist} • {track.AlbumTitle}";
        }
        else
        {
            TxtArtist.Text = string.IsNullOrWhiteSpace(track.Artist) ? "Unknown Artist" : track.Artist;
        }

        // Hiển thị Thumbnail hoặc icon dự phòng
        if (track.Thumbnail != null)
        {
            ImgThumbnail.Source = track.Thumbnail;
            ImgThumbnail.Visibility = Visibility.Visible;
            PlaceholderIcon.Visibility = Visibility.Collapsed;
        }
        else
        {
            ImgThumbnail.Source = null;
            ImgThumbnail.Visibility = Visibility.Collapsed;
            PlaceholderIcon.Visibility = Visibility.Visible;
        }

        // Cập nhật vị trí cửa sổ theo config
        CalculatePosition();

        // Kích hoạt Animation xuất hiện mượt mà
        ShowWithAnimation();
    }

    private void CalculatePosition()
    {
        var workArea = SystemParameters.WorkArea;
        double x = 0;
        double y = 0;

        string alignment = _config.Position.Alignment?.Trim().ToLowerInvariant() ?? "topright";

        switch (alignment)
        {
            case "topright":
                x = workArea.Right - Width - _config.Position.OffsetX;
                y = workArea.Top + _config.Position.OffsetY;
                break;

            case "topcenter":
                x = workArea.Left + (workArea.Width - Width) / 2;
                y = workArea.Top + _config.Position.OffsetY;
                break;

            case "topleft":
                x = workArea.Left + _config.Position.OffsetX;
                y = workArea.Top + _config.Position.OffsetY;
                break;

            case "bottomright":
                x = workArea.Right - Width - _config.Position.OffsetX;
                y = workArea.Bottom - Height - _config.Position.OffsetY;
                break;

            case "bottomleft":
                x = workArea.Left + _config.Position.OffsetX;
                y = workArea.Bottom - Height - _config.Position.OffsetY;
                break;

            case "custom":
                x = _config.Position.CustomX;
                y = _config.Position.CustomY;
                break;

            default: // Mặc định TopRight
                x = workArea.Right - Width - _config.Position.OffsetX;
                y = workArea.Top + _config.Position.OffsetY;
                break;
        }

        Left = Math.Max(0, x);
        Top = Math.Max(0, y);
    }

    private void ShowWithAnimation()
    {
        _isHiding = false;
        _hideTimer.Stop();

        // Hiển thị Window nếu đang ẩn
        if (!IsVisible)
        {
            Show();
        }

        var animDuration = TimeSpan.FromMilliseconds(Math.Max(100, _config.AnimationDurationMs));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        // Animation Fade-in
        var fadeIn = new DoubleAnimation(0.0, 1.0, animDuration) { EasingFunction = ease };
        BeginAnimation(OpacityProperty, fadeIn);

        // Animation Slide-in phụ thuộc vào hướng căn lề
        string alignment = _config.Position.Alignment?.Trim().ToLowerInvariant() ?? "topright";
        if (alignment.Contains("right"))
        {
            var slideX = new DoubleAnimation(40.0, 0.0, animDuration) { EasingFunction = ease };
            WindowTranslateTransform.BeginAnimation(TranslateTransform.XProperty, slideX);
            WindowTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
        }
        else if (alignment.Contains("left"))
        {
            var slideX = new DoubleAnimation(-40.0, 0.0, animDuration) { EasingFunction = ease };
            WindowTranslateTransform.BeginAnimation(TranslateTransform.XProperty, slideX);
            WindowTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
        }
        else
        {
            var slideY = new DoubleAnimation(-30.0, 0.0, animDuration) { EasingFunction = ease };
            WindowTranslateTransform.BeginAnimation(TranslateTransform.YProperty, slideY);
            WindowTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
        }

        // Bắt đầu đếm thời gian hiển thị
        _hideTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1000, _config.DisplayDurationMs));
        _hideTimer.Start();
    }

    public void HideWithAnimation()
    {
        if (_isHiding || !IsVisible) return;
        _isHiding = true;

        var animDuration = TimeSpan.FromMilliseconds(Math.Max(100, _config.AnimationDurationMs));
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        // Animation Fade-out
        var fadeOut = new DoubleAnimation(Opacity, 0.0, animDuration) { EasingFunction = ease };

        // Animation Slide-out
        string alignment = _config.Position.Alignment?.Trim().ToLowerInvariant() ?? "topright";
        if (alignment.Contains("right"))
        {
            var slideX = new DoubleAnimation(WindowTranslateTransform.X, 35.0, animDuration) { EasingFunction = ease };
            WindowTranslateTransform.BeginAnimation(TranslateTransform.XProperty, slideX);
        }
        else if (alignment.Contains("left"))
        {
            var slideX = new DoubleAnimation(WindowTranslateTransform.X, -35.0, animDuration) { EasingFunction = ease };
            WindowTranslateTransform.BeginAnimation(TranslateTransform.XProperty, slideX);
        }
        else
        {
            var slideY = new DoubleAnimation(WindowTranslateTransform.Y, -25.0, animDuration) { EasingFunction = ease };
            WindowTranslateTransform.BeginAnimation(TranslateTransform.YProperty, slideY);
        }

        fadeOut.Completed += (s, e) =>
        {
            if (_isHiding)
            {
                Hide();
                _isHiding = false;
            }
        };

        BeginAnimation(OpacityProperty, fadeOut);
    }

    private async void BtnSkip_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _mediaService.TrySkipNextAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Lỗi Skip: {ex.Message}");
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        _hideTimer.Stop();
        HideWithAnimation();
    }
}
