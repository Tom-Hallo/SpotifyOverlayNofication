using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace SpotifyNowPlaying.Services;

/// <summary>
/// Đại diện cho bộ màu nền Glassmorphism phong cách Apple,
/// được tính toán hài hòa, dịu mắt từ ảnh bìa bài hát.
/// </summary>
public class AppleGlassPalette
{
    // Màu gradient nền chính (tối, dịu, bão hòa thấp đến vừa phải)
    public Color BaseStart { get; init; }
    public Color BaseMid { get; init; }
    public Color BaseEnd { get; init; }

    // Màu ánh sáng Ambient dịu nhẹ tỏa ra từ ảnh bìa
    public Color AmbientGlow { get; init; }
    public Color AmbientGlowMid { get; init; }

    // Màu viền phản quang specular highlight chuẩn Apple glass
    public Color BorderTop { get; init; }
    public Color BorderBottom { get; init; }

    /// <summary>
    /// Bảng màu mặc định phong cách Dark Glass thanh lịch (khi chưa có ảnh bìa hoặc bài hát không có cover).
    /// </summary>
    public static AppleGlassPalette Default => new()
    {
        BaseStart = Color.FromArgb(242, 34, 37, 44),       // #22252C (Graphite Slate)
        BaseMid = Color.FromArgb(245, 23, 24, 29),         // #17181D
        BaseEnd = Color.FromArgb(248, 16, 17, 21),         // #101115 (Deep Obsidian)
        AmbientGlow = Color.FromArgb(38, 70, 95, 140),     // Ánh xanh băng dịu nhẹ
        AmbientGlowMid = Color.FromArgb(14, 40, 55, 80),
        BorderTop = Color.FromArgb(52, 255, 255, 255),
        BorderBottom = Color.FromArgb(12, 255, 255, 255)
    };
}

/// <summary>
/// Service trích xuất và biến đổi màu chủ đạo từ ảnh bìa (Avatar) thành bảng màu
/// Ambient Glassmorphism phong cách Apple: sang trọng, dịu mắt, độ tương phản cao.
/// </summary>
public static class PaletteExtractor
{
    /// <summary>
    /// Trích xuất bảng màu kính Apple từ BitmapSource với hiệu năng cao (< 0.1ms).
    /// </summary>
    public static AppleGlassPalette ExtractPalette(BitmapSource? image)
    {
        if (image == null)
        {
            return AppleGlassPalette.Default;
        }

        try
        {
            // Đảm bảo định dạng Bgra32 để đọc pixel an toàn, không phụ thuộc pixel format gốc
            BitmapSource formatted = image;
            if (formatted.Format != PixelFormats.Bgra32)
            {
                formatted = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
            }

            int targetSize = 24;
            BitmapSource sampledSource = formatted;

            // Thu nhỏ ảnh về 24x24 pixel để lấy mẫu siêu nhẹ và tức thì
            if (formatted.PixelWidth > targetSize || formatted.PixelHeight > targetSize)
            {
                double scaleX = (double)targetSize / Math.Max(1, formatted.PixelWidth);
                double scaleY = (double)targetSize / Math.Max(1, formatted.PixelHeight);
                sampledSource = new TransformedBitmap(formatted, new ScaleTransform(scaleX, scaleY));
            }

            int width = sampledSource.PixelWidth;
            int height = sampledSource.PixelHeight;
            if (width <= 0 || height <= 0)
            {
                return AppleGlassPalette.Default;
            }

            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            sampledSource.CopyPixels(pixels, stride, 0);

            // Gom nhóm màu theo 12 Hue bucket và 1 nhóm trung tính (Neutral/Gray)
            const int bucketCount = 12;
            double[] bucketWeights = new double[bucketCount];
            double[] bucketSin = new double[bucketCount];
            double[] bucketCos = new double[bucketCount];
            double[] bucketSat = new double[bucketCount];
            double[] bucketLight = new double[bucketCount];

            double neutralWeight = 0;
            double neutralLight = 0;

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                if (a < 128) continue; // Bỏ qua pixel trong suốt

                RgbToHsl(r, g, b, out double h, out double s, out double l);

                // Loại trừ các pixel quá tối (<0.07) hoặc quá chói trắng (>0.93)
                if (l < 0.07 || l > 0.93) continue;

                // Nếu độ bão hòa quá thấp, xếp vào nhóm trung tính
                if (s < 0.12)
                {
                    double w = 1.0;
                    neutralWeight += w;
                    neutralLight += l * w;
                    continue;
                }

                // Trọng số ưu tiên màu sắc có độ bão hòa vừa phải (nghệ thuật, đặc trưng bìa nhạc)
                double weight = 1.0 + s * 2.2;
                int bucketIndex = (int)(h / 30.0) % bucketCount;
                if (bucketIndex < 0) bucketIndex += bucketCount;

                double rad = h * Math.PI / 180.0;
                bucketWeights[bucketIndex] += weight;
                bucketSin[bucketIndex] += Math.Sin(rad) * weight;
                bucketCos[bucketIndex] += Math.Cos(rad) * weight;
                bucketSat[bucketIndex] += s * weight;
                bucketLight[bucketIndex] += l * weight;
            }

            // Tìm bucket có tổng trọng số cao nhất
            int bestBucket = -1;
            double maxWeight = 0;
            for (int i = 0; i < bucketCount; i++)
            {
                if (bucketWeights[i] > maxWeight)
                {
                    maxWeight = bucketWeights[i];
                    bestBucket = i;
                }
            }

            double finalH, finalS, finalL;

            if (bestBucket == -1 || maxWeight < 1.0)
            {
                // Nếu ảnh đơn sắc / đen trắng: dùng tông xám than chì lạnh Apple Slate
                finalH = 220.0;
                finalS = 0.08;
                finalL = neutralWeight > 0 ? Math.Clamp(neutralLight / neutralWeight, 0.14, 0.35) : 0.22;
            }
            else
            {
                double avgSin = bucketSin[bestBucket] / bucketWeights[bestBucket];
                double avgCos = bucketCos[bestBucket] / bucketWeights[bestBucket];
                double angle = Math.Atan2(avgSin, avgCos) * 180.0 / Math.PI;
                if (angle < 0) angle += 360.0;

                finalH = angle;
                finalS = bucketSat[bestBucket] / bucketWeights[bestBucket];
                finalL = bucketLight[bestBucket] / bucketWeights[bestBucket];
            }

            return GenerateApplePalette(finalH, finalS, finalL);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PaletteExtractor] Lỗi trích xuất màu: {ex.Message}");
            return AppleGlassPalette.Default;
        }
    }

    /// <summary>
    /// Biến đổi HSL thô thành các tông màu phong cách Apple:
    /// - Tông trầm tối, bão hòa vừa phải (không bao giờ neon hay quá rực).
    /// - Độ sáng dễ chịu, tối ưu độ tương phản cho chữ trắng và xám bạc.
    /// - Ánh sáng Ambient phản chiếu tự nhiên từ bìa đĩa.
    /// </summary>
    public static AppleGlassPalette GenerateApplePalette(double h, double s, double l)
    {
        // 1. Giới hạn độ bão hòa dịu mắt: 0.14 - 0.28 (Apple Vibrant Dark Material)
        double mutedSat = Math.Clamp(s * 0.55, 0.14, 0.28);

        // 2. Tông bắt đầu (Góc trên-trái gần avatar): Phảng phất sắc thái bìa album, độ sáng tối êm dịu (~0.165)
        Color baseStart = HslToRgb(h, mutedSat, 0.165, alpha: 242);

        // 3. Tông giữa (Chuyển tiếp tự nhiên sang tối): Giảm bớt độ bão hòa (~0.115)
        Color baseMid = HslToRgb(h, mutedSat * 0.65, 0.115, alpha: 245);

        // 4. Tông cuối (Góc dưới-phải): Tông obsidian tối sâu thẳm sang trọng (~0.075)
        Color baseEnd = HslToRgb(h, mutedSat * 0.30, 0.075, alpha: 248);

        // 5. Ánh sáng Ambient dịu nhẹ tỏa ra từ góc ảnh bìa:
        double ambientSat = Math.Clamp(s * 0.70, 0.20, 0.38);
        Color ambientGlow = HslToRgb(h, ambientSat, 0.32, alpha: 46);
        Color ambientGlowMid = HslToRgb(h, ambientSat * 0.60, 0.20, alpha: 15);

        return new AppleGlassPalette
        {
            BaseStart = baseStart,
            BaseMid = baseMid,
            BaseEnd = baseEnd,
            AmbientGlow = ambientGlow,
            AmbientGlowMid = ambientGlowMid,
            BorderTop = Color.FromArgb(52, 255, 255, 255),
            BorderBottom = Color.FromArgb(12, 255, 255, 255)
        };
    }

    /// <summary>
    /// Tạo bảng màu Apple Glass dựa trên mã màu hex tùy chỉnh (ví dụ khi người dùng cấu hình BackgroundColor).
    /// </summary>
    public static AppleGlassPalette CreateFromHex(string hexColor)
    {
        try
        {
            var rawColor = (Color)ColorConverter.ConvertFromString(hexColor);
            RgbToHsl(rawColor.R, rawColor.G, rawColor.B, out double h, out double s, out double l);
            return GenerateApplePalette(h, s, l);
        }
        catch
        {
            return AppleGlassPalette.Default;
        }
    }

    #region Chuyển đổi không gian màu RGB <-> HSL chuẩn xác
    public static void RgbToHsl(byte r, byte g, byte b, out double h, out double s, out double l)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        l = (max + min) / 2.0;

        if (delta < 0.00001)
        {
            h = 0;
            s = 0;
            return;
        }

        s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

        if (Math.Abs(max - rd) < 0.00001)
        {
            h = ((gd - bd) / delta) + (gd < bd ? 6.0 : 0.0);
        }
        else if (Math.Abs(max - gd) < 0.00001)
        {
            h = ((bd - rd) / delta) + 2.0;
        }
        else
        {
            h = ((rd - gd) / delta) + 4.0;
        }

        h *= 60.0;
    }

    public static Color HslToRgb(double h, double s, double l, byte alpha = 255)
    {
        double r, g, b;

        if (s < 0.00001)
        {
            r = g = b = l;
        }
        else
        {
            double q = l < 0.5 ? l * (1.0 + s) : l + s - (l * s);
            double p = 2.0 * l - q;
            double hk = (h % 360.0) / 360.0;
            if (hk < 0) hk += 1.0;

            r = HueToRgb(p, q, hk + 1.0 / 3.0);
            g = HueToRgb(p, q, hk);
            b = HueToRgb(p, q, hk - 1.0 / 3.0);
        }

        return Color.FromArgb(
            alpha,
            (byte)Math.Clamp((int)Math.Round(r * 255.0), 0, 255),
            (byte)Math.Clamp((int)Math.Round(g * 255.0), 0, 255),
            (byte)Math.Clamp((int)Math.Round(b * 255.0), 0, 255)
        );
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1.0;
        if (t > 1) t -= 1.0;
        if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        return p;
    }
    #endregion
}
