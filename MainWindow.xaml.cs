using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;
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
        CalculatePosition();
    }

    private void ApplyStyling()
    {
        try
        {
            // 1. Kích thước Window tổng thể
            double targetWidth = Math.Max(260, _config.Style.Width);
            double targetHeight = Math.Max(60, _config.Style.Height);

            Width = targetWidth;
            Height = targetHeight;

            // 2. Bo góc riêng biệt cho Left và Right
            double leftRadius = _config.Style.GetLeftRadius();
            double rightRadius = _config.Style.GetRightRadius();

            // Cấu trúc CornerRadius trong WPF: TopLeft, TopRight, BottomRight, BottomLeft
            var cr = new CornerRadius(leftRadius, rightRadius, rightRadius, leftRadius);
            CardBorder.CornerRadius = cr;
            GlassCard.CornerRadius = cr;

            // 3. Xử lý viền kính và lề để áp sát hoặc tách rời cạnh màn hình tự nhiên
            string alignment = _config.Position.Alignment?.Trim().ToLowerInvariant() ?? "topright";
            bool isRightAligned = alignment.Contains("right");
            bool isLeftAligned = alignment.Contains("left");

            double leftMargin = 14;
            double rightMargin = 0;

            if (isRightAligned)
            {
                // Nếu góc phải có bo tròn hoặc có lùi lề OffsetX > 0 -> cấp margin phải để không bị cắt bóng đổ DropShadow
                rightMargin = (rightRadius > 0 && _config.Position.OffsetX > 0) ? 14 : 0;
                leftMargin = 14;
                double rightBorderThick = (rightRadius > 0 || _config.Position.OffsetX > 0) ? 1.2 : 0;
                GlassCard.BorderThickness = new Thickness(1.2, 1.2, rightBorderThick, 1.2);
            }
            else if (isLeftAligned)
            {
                leftMargin = (leftRadius > 0 && _config.Position.OffsetX > 0) ? 14 : 0;
                rightMargin = 14;
                double leftBorderThick = (leftRadius > 0 || _config.Position.OffsetX > 0) ? 1.2 : 0;
                GlassCard.BorderThickness = new Thickness(leftBorderThick, 1.2, 1.2, 1.2);
            }
            else
            {
                leftMargin = 14;
                rightMargin = 14;
                GlassCard.BorderThickness = new Thickness(1.2);
            }

            CardBorder.Margin = new Thickness(leftMargin, 8, rightMargin, 8);

            // Dải màu Accent Bar uốn lượn theo bo góc trái
            AccentBar.CornerRadius = new CornerRadius(Math.Max(0, leftRadius - 2), 0, 0, Math.Max(0, leftRadius - 2));

            // 4. Tự động co giãn (Responsive Layout) các phần tử theo Height để không bao giờ bị tràn/vỡ giao diện
            double innerCardHeight = Math.Max(40, targetHeight - 16); // trừ margin trên dưới (8+8)
            double vertPadding = Math.Clamp((innerCardHeight - 64) / 4, 4, 12);
            ContentGrid.Margin = new Thickness(14, vertPadding, 14, vertPadding);

            double maxThumb = Math.Max(32, innerCardHeight - (vertPadding * 2));
            double thumbSize = Math.Clamp(maxThumb, 36, 76);
            ThumbnailBorder.Width = thumbSize;
            ThumbnailBorder.Height = thumbSize;
            ThumbnailBorder.CornerRadius = new CornerRadius(Math.Clamp(thumbSize * 0.16, 5, 12));

            // Tỉ lệ nút bấm theo chiều cao thẻ
            double skipBtnSize = Math.Clamp(thumbSize * 0.58, 28, 40);
            BtnSkip.Width = skipBtnSize;
            BtnSkip.Height = skipBtnSize;

            double closeBtnSize = Math.Clamp(skipBtnSize * 0.72, 22, 28);
            BtnClose.Width = closeBtnSize;
            BtnClose.Height = closeBtnSize;

            // Kích thước font chữ co giãn linh hoạt
            if (targetHeight < 80)
            {
                TxtTitle.FontSize = 12.5;
                TxtArtist.FontSize = 10.5;
                TxtAppLabel.FontSize = 8.5;
                SpotifyIcon.Width = 11;
                SpotifyIcon.Height = 11;
            }
            else if (targetHeight < 95)
            {
                TxtTitle.FontSize = 13.5;
                TxtArtist.FontSize = 11.5;
                TxtAppLabel.FontSize = 9.5;
                SpotifyIcon.Width = 12;
                SpotifyIcon.Height = 12;
            }
            else
            {
                TxtTitle.FontSize = 15;
                TxtArtist.FontSize = 13;
                TxtAppLabel.FontSize = 10;
                SpotifyIcon.Width = 13;
                SpotifyIcon.Height = 13;
            }

            // Chiều cao lớp gương bóng mờ chiếm ~45% chiều cao thẻ
            GlossySheen.Height = Math.Max(18, innerCardHeight * 0.45);

            // Nền trong suốt trên CardBorder để bóng đổ mềm ôm trọn GlassCard
            CardBorder.Background = Brushes.Transparent;

            // Áp dụng bảng màu nền mặc định ban đầu
            var initialPalette = !string.IsNullOrWhiteSpace(_config.Style.BackgroundColor) && _config.Style.BackgroundColor != "#E6181818"
                ? PaletteExtractor.CreateFromHex(_config.Style.BackgroundColor)
                : AppleGlassPalette.Default;
            ApplyPalette(initialPalette, animate: false);

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

    private void ApplyPalette(AppleGlassPalette palette, bool animate)
    {
        if (!animate)
        {
            // Gán trực tiếp không qua animation (khi mới mở flyout hoặc khởi động)
            BgGradStop0.BeginAnimation(GradientStop.ColorProperty, null);
            BgGradStop1.BeginAnimation(GradientStop.ColorProperty, null);
            BgGradStop2.BeginAnimation(GradientStop.ColorProperty, null);
            AmbientGradStop0.BeginAnimation(GradientStop.ColorProperty, null);
            AmbientGradStop1.BeginAnimation(GradientStop.ColorProperty, null);
            BorderGradStop0.BeginAnimation(GradientStop.ColorProperty, null);
            BorderGradStop1.BeginAnimation(GradientStop.ColorProperty, null);

            BgGradStop0.Color = palette.BaseStart;
            BgGradStop1.Color = palette.BaseMid;
            BgGradStop2.Color = palette.BaseEnd;
            AmbientGradStop0.Color = palette.AmbientGlow;
            AmbientGradStop1.Color = palette.AmbientGlowMid;
            BorderGradStop0.Color = palette.BorderTop;
            BorderGradStop1.Color = palette.BorderBottom;
        }
        else
        {
            // Hiệu ứng chuyển tiếp màu mềm mại chuẩn Apple (450ms EaseOut)
            var duration = TimeSpan.FromMilliseconds(450);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            AnimateColor(BgGradStop0, palette.BaseStart, duration, ease);
            AnimateColor(BgGradStop1, palette.BaseMid, duration, ease);
            AnimateColor(BgGradStop2, palette.BaseEnd, duration, ease);
            AnimateColor(AmbientGradStop0, palette.AmbientGlow, duration, ease);
            AnimateColor(AmbientGradStop1, palette.AmbientGlowMid, duration, ease);
            AnimateColor(BorderGradStop0, palette.BorderTop, duration, ease);
            AnimateColor(BorderGradStop1, palette.BorderBottom, duration, ease);
        }
    }

    private static void AnimateColor(GradientStop stop, Color targetColor, TimeSpan duration, IEasingFunction ease)
    {
        var anim = new ColorAnimation(targetColor, duration) { EasingFunction = ease };
        stop.BeginAnimation(GradientStop.ColorProperty, anim);
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

        // Trích xuất màu chủ đạo từ ảnh bìa và áp dụng bảng màu Apple Glassmorphism
        var targetPalette = PaletteExtractor.ExtractPalette(track.Thumbnail);
        bool shouldAnimate = IsVisible && Opacity > 0.05 && !_isHiding;
        ApplyPalette(targetPalette, animate: shouldAnimate);

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
