using System.IO;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using SpotifyNowPlaying.Models;

namespace SpotifyNowPlaying.Services;

public class SpotifyMediaService
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _spotifySession;

    // Khóa tránh xử lý trùng lặp và race condition khi người dùng skip bài liên tục
    private string _lastTrackKey = string.Empty;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public event Action<TrackInfo>? TrackChanged;

    public async Task InitializeAsync()
    {
        Disconnect();

        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager == null)
            {
                System.Diagnostics.Debug.WriteLine("[SpotifyMediaService] Không thể kết nối GSMTC SessionManager.");
                return;
            }

            _sessionManager.SessionsChanged += OnSessionsChanged;
            _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

            await UpdateSpotifySessionAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SpotifyMediaService] Lỗi khởi tạo: {ex.Message}");
        }
    }

    private async void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        await UpdateSpotifySessionAsync();
    }

    private async void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        await UpdateSpotifySessionAsync();
    }

    private async Task UpdateSpotifySessionAsync()
    {
        if (_sessionManager == null) return;

        try
        {
            var sessions = _sessionManager.GetSessions();
            var spotify = sessions.FirstOrDefault(s =>
                !string.IsNullOrEmpty(s.SourceAppUserModelId) &&
                s.SourceAppUserModelId.Contains("Spotify", StringComparison.OrdinalIgnoreCase));

            if (spotify != _spotifySession)
            {
                if (_spotifySession != null)
                {
                    _spotifySession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                }

                _spotifySession = spotify;

                if (_spotifySession != null)
                {
                    _spotifySession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                    await ProcessCurrentTrackAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SpotifyMediaService] Lỗi cập nhật session: {ex.Message}");
        }
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await ProcessCurrentTrackAsync();
    }

    public async Task ProcessCurrentTrackAsync()
    {
        if (_spotifySession == null) return;

        // Tránh nhiều luồng cùng gọi đồng thời khi chuyển bài nhanh
        if (!await _lock.WaitAsync(500)) return;

        try
        {
            var props = await _spotifySession.TryGetMediaPropertiesAsync();
            if (props == null) return;

            string title = props.Title?.Trim() ?? string.Empty;
            string artist = props.Artist?.Trim() ?? string.Empty;
            string album = props.AlbumTitle?.Trim() ?? string.Empty;

            // Nếu không có thông tin bài hát (chưa phát hoặc đang tải)
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
            {
                return;
            }

            // Kiểm tra key tránh trigger trùng lặp nếu bài hát chưa đổi
            string trackKey = $"{title}|{artist}|{album}";
            if (trackKey == _lastTrackKey)
            {
                return;
            }

            _lastTrackKey = trackKey;

            // Trích xuất Thumbnail stream
            BitmapImage? thumbnailBitmap = null;
            if (props.Thumbnail != null)
            {
                thumbnailBitmap = await LoadThumbnailAsync(props.Thumbnail);
            }

            var trackInfo = new TrackInfo
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Unknown Title" : title,
                Artist = string.IsNullOrWhiteSpace(artist) ? "Unknown Artist" : artist,
                AlbumTitle = album,
                Thumbnail = thumbnailBitmap
            };

            TrackChanged?.Invoke(trackInfo);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SpotifyMediaService] Lỗi đọc MediaProperties: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task<BitmapImage?> LoadThumbnailAsync(IRandomAccessStreamReference streamRef)
    {
        try
        {
            using var streamWithContent = await streamRef.OpenReadAsync();
            if (streamWithContent == null || streamWithContent.Size == 0) return null;

            using var netStream = streamWithContent.AsStreamForRead();
            using var memoryStream = new MemoryStream();
            await netStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CreateOptions = BitmapCreateOptions.None;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();

            // Freeze để có thể truyền và sử dụng an toàn trên UI thread
            if (bitmap.CanFreeze)
            {
                bitmap.Freeze();
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SpotifyMediaService] Không tải được thumbnail: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> TrySkipNextAsync()
    {
        if (_spotifySession == null) return false;

        try
        {
            return await _spotifySession.TrySkipNextAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SpotifyMediaService] Lỗi khi Skip: {ex.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        try
        {
            if (_spotifySession != null)
            {
                _spotifySession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _spotifySession = null;
            }

            if (_sessionManager != null)
            {
                _sessionManager.SessionsChanged -= OnSessionsChanged;
                _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
                _sessionManager = null;
            }

            _lastTrackKey = string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SpotifyMediaService] Lỗi khi Disconnect: {ex.Message}");
        }
    }
}
