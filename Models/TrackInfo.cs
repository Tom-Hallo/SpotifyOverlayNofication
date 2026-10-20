using System.Windows.Media.Imaging;

namespace SpotifyNowPlaying.Models;

public class TrackInfo
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public BitmapImage? Thumbnail { get; set; }
}
