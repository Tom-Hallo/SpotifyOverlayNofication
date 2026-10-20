using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using SpotifyNowPlaying.Models;
using SpotifyNowPlaying.Services;

namespace SpotifyNowPlaying;

public partial class App : System.Windows.Application
{
    private static Mutex? _singleInstanceMutex;

    private Forms.NotifyIcon? _notifyIcon;
    private ConfigService? _configService;
    private SpotifyMediaService? _mediaService;
    private SpotifyLifecycleService? _lifecycleService;
    private MainWindow? _mainWindow;

    private Forms.ToolStripMenuItem? _syncMenuItem;
    private Forms.ToolStripMenuItem? _startupMenuItem;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Đảm bảo chỉ có một phiên bản ứng dụng duy nhất chạy (Single Instance)
        const string mutexName = @"Local\SpotifyNowPlaying_SingleInstance_Mutex";
        _singleInstanceMutex = new Mutex(true, mutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            Shutdown();
            return;
        }

        bool isStartupLaunch = e.Args.Any(a => a.Equals("--startup", StringComparison.OrdinalIgnoreCase));

        // 1. Khởi tạo Service quản lý cấu hình
        _configService = new ConfigService();
        _configService.LoadConfig();

        // Đồng bộ trạng thái Startup với Windows theo config nếu cần
        if (_configService.CurrentConfig.AutoLifeCycle.StartWithWindows && !StartupService.IsStartupEnabled())
        {
            StartupService.SetStartup(true);
        }

        // 2. Khởi tạo Service lắng nghe GSMTC từ Spotify
        _mediaService = new SpotifyMediaService();

        // 3. Khởi tạo Toast Window (chạy ngầm)
        _mainWindow = new MainWindow(_mediaService, _configService.CurrentConfig);

        // Lắng nghe sự kiện chuyển bài hát
        _mediaService.TrackChanged += OnTrackChanged;

        // Lắng nghe sự kiện thay đổi cấu hình runtime
        _configService.ConfigChanged += OnConfigChanged;

        // 4. Khởi tạo Service theo dõi vòng đời của Spotify
        _lifecycleService = new SpotifyLifecycleService(_configService.CurrentConfig.AutoLifeCycle.CheckIntervalMs);
        _lifecycleService.SpotifyStarted += OnSpotifyStarted;
        _lifecycleService.SpotifyExited += OnSpotifyExited;

        // 5. Khởi tạo System Tray Icon (Khay hệ thống)
        InitializeTrayIcon();

        // 6. Bắt đầu theo dõi Spotify
        _lifecycleService.Start();

        bool isSpotifyRunning = _lifecycleService.IsSpotifyRunning;
        bool autoSync = _configService.CurrentConfig.AutoLifeCycle.AutoSyncWithSpotify;

        if (autoSync)
        {
            if (isSpotifyRunning)
            {
                // Spotify đang chạy -> Kích hoạt ngay lập tức
                await ActivateAppAsync();
            }
            else
            {
                // Spotify chưa chạy -> Vào chế độ ngủ ngầm
                if (!isStartupLaunch)
                {
                    // Người dùng bấm mở app thủ công: Hiện thông báo 3.5s rồi mới ẩn vào ngủ ngầm
                    ShowInitialStandbyNotice();
                }
                else
                {
                    // Khởi động cùng Windows: Ngủ ngầm yên lặng hoàn toàn
                    StandbyApp(silent: true);
                }
            }
        }
        else
        {
            // Tắt tính năng tự động đồng bộ: Luôn kích hoạt
            await ActivateAppAsync();
        }
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Spotify Now Playing Overlay",
            Visible = true
        };

        var contextMenu = new Forms.ContextMenuStrip();

        var reloadItem = new Forms.ToolStripMenuItem("Reload Config", null, (s, e) =>
        {
            _configService?.LoadConfig();
            _notifyIcon?.ShowBalloonTip(1500, "Spotify Overlay", "Config đã được nạp lại thành công!", Forms.ToolTipIcon.Info);
        });

        var testToastItem = new Forms.ToolStripMenuItem("Test Toast", null, (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                _mainWindow?.ShowTrack(new TrackInfo
                {
                    Title = "Never Gonna Give You Up",
                    Artist = "Rick Astley",
                    AlbumTitle = "Whenever You Need Somebody",
                    Thumbnail = null
                });
            });
        });

        _syncMenuItem = new Forms.ToolStripMenuItem("Tự động đồng bộ theo Spotify", null, (s, e) =>
        {
            if (_configService == null || _syncMenuItem == null) return;
            bool newState = !_syncMenuItem.Checked;
            _syncMenuItem.Checked = newState;

            _configService.CurrentConfig.AutoLifeCycle.AutoSyncWithSpotify = newState;
            _configService.SaveConfig(_configService.CurrentConfig);

            if (newState)
            {
                if (_lifecycleService != null && !_lifecycleService.IsSpotifyRunning)
                {
                    StandbyApp(silent: false);
                }
            }
            else
            {
                _ = ActivateAppAsync();
            }
        })
        {
            CheckOnClick = true,
            Checked = _configService?.CurrentConfig.AutoLifeCycle.AutoSyncWithSpotify ?? true
        };

        _startupMenuItem = new Forms.ToolStripMenuItem("Khởi động cùng Windows", null, (s, e) =>
        {
            if (_configService == null || _startupMenuItem == null) return;
            bool newState = !_startupMenuItem.Checked;
            _startupMenuItem.Checked = newState;

            StartupService.SetStartup(newState);
            _configService.CurrentConfig.AutoLifeCycle.StartWithWindows = newState;
            _configService.SaveConfig(_configService.CurrentConfig);

            string msg = newState
                ? "Ứng dụng sẽ tự động khởi động cùng Windows ở chế độ chờ."
                : "Đã hủy khởi động cùng Windows.";
            _notifyIcon?.ShowBalloonTip(1500, "Spotify Overlay", msg, Forms.ToolTipIcon.Info);
        })
        {
            CheckOnClick = true,
            Checked = StartupService.IsStartupEnabled()
        };

        var exitItem = new Forms.ToolStripMenuItem("Exit (Thoát hoàn toàn)", null, (s, e) =>
        {
            ExitApp();
        });

        contextMenu.Items.Add(reloadItem);
        contextMenu.Items.Add(testToastItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(_syncMenuItem);
        contextMenu.Items.Add(_startupMenuItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void ShowInitialStandbyNotice()
    {
        if (_notifyIcon == null) return;

        _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(3000, "Spotify Now Playing", "Ứng dụng đang ở chế độ chờ và sẽ tự động kích hoạt ngay khi bạn mở Spotify!", Forms.ToolTipIcon.Info);

        // Sau 3.5 giây tự động ẩn System Tray icon để vào chế độ ngủ ngầm hoàn toàn
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
        timer.Tick += (s, ev) =>
        {
            timer.Stop();
            if (_lifecycleService != null && !_lifecycleService.IsSpotifyRunning &&
                (_configService?.CurrentConfig.AutoLifeCycle.AutoSyncWithSpotify ?? true))
            {
                StandbyApp(silent: true);
            }
        };
        timer.Start();
    }

    private async Task ActivateAppAsync()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Spotify Now Playing (Đang hoạt động)";
        }

        if (_mediaService != null)
        {
            await _mediaService.InitializeAsync();
        }
    }

    private void StandbyApp(bool silent = false)
    {
        Dispatcher.Invoke(() =>
        {
            // Ẩn Toast đang hiển thị nếu có
            _mainWindow?.HideWithAnimation();

            // Ngắt kết nối lắng nghe GSMTC để tránh xử lý dư thừa
            _mediaService?.Disconnect();

            if (!silent && _notifyIcon != null && _notifyIcon.Visible)
            {
                _notifyIcon.ShowBalloonTip(1500, "Spotify Now Playing", "Spotify đã tắt. Ứng dụng đã chuyển sang chế độ ngủ ngầm.", Forms.ToolTipIcon.Info);
            }

            // Ẩn biểu tượng khay hệ thống hoàn toàn
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }

            // Giải phóng bộ nhớ RAM tối đa khi ngủ
            SpotifyLifecycleService.TrimMemory();
        });
    }

    private async void OnSpotifyStarted()
    {
        if (!(_configService?.CurrentConfig.AutoLifeCycle.AutoSyncWithSpotify ?? true)) return;

        await Dispatcher.InvokeAsync(async () =>
        {
            await ActivateAppAsync();

            _notifyIcon?.ShowBalloonTip(2000, "Spotify Now Playing", "Spotify đã mở! Ứng dụng đã tự động kích hoạt.", Forms.ToolTipIcon.Info);
        });
    }

    private void OnSpotifyExited()
    {
        if (!(_configService?.CurrentConfig.AutoLifeCycle.AutoSyncWithSpotify ?? true)) return;

        StandbyApp(silent: false);
    }

    private void OnTrackChanged(TrackInfo track)
    {
        Dispatcher.Invoke(() =>
        {
            _mainWindow?.ShowTrack(track);
        });
    }

    private void OnConfigChanged(AppConfig newConfig)
    {
        Dispatcher.Invoke(() =>
        {
            _mainWindow?.UpdateConfig(newConfig);

            if (_lifecycleService != null)
            {
                _lifecycleService.UpdateInterval(newConfig.AutoLifeCycle.CheckIntervalMs);
            }

            if (_syncMenuItem != null)
            {
                _syncMenuItem.Checked = newConfig.AutoLifeCycle.AutoSyncWithSpotify;
            }

            if (_startupMenuItem != null)
            {
                _startupMenuItem.Checked = StartupService.IsStartupEnabled();
            }
        });
    }

    private void ExitApp()
    {
        _lifecycleService?.Dispose();
        _lifecycleService = null;

        _mediaService?.Disconnect();
        _mediaService = null;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _mainWindow?.Close();

        if (_singleInstanceMutex != null)
        {
            try { _singleInstanceMutex.ReleaseMutex(); } catch { }
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _lifecycleService?.Dispose();
        _mediaService?.Disconnect();

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        if (_singleInstanceMutex != null)
        {
            try { _singleInstanceMutex.ReleaseMutex(); } catch { }
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        base.OnExit(e);
    }
}
