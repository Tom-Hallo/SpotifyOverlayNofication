<div align="center">

# 🎵 Spotify Now Playing Toast Overlay

**A modern, ultra-lightweight, and non-intrusive "Now Playing" desktop notification flyout for Spotify on Windows 10 & 11.**  
*Ứng dụng thông báo bài hát Spotify đang phát hiện đại, siêu nhẹ, không chiếm quyền điều khiển trên Windows 10 & 11.*

<br/>

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Language](https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Spotify](https://img.shields.io/badge/Spotify-GSMTC%20WinRT-1DB954?style=for-the-badge&logo=spotify&logoColor=white)](https://spotify.com)
[![License](https://img.shields.io/badge/License-MIT-blue.badge?style=for-the-badge)](LICENSE)

<br/>

[English](#english) • [Tiếng Việt](#tiếng-việt)

</div>

---

<a name="english"></a>
# 🇬🇧 English

## 📖 Overview

**Spotify Now Playing Toast Overlay** is a minimalist Windows background utility built with **C# .NET 8 WPF** and native **Windows Runtime (WinRT) Global System Media Transport Controls (GSMTC)** APIs.

Whenever Spotify switches songs, a sleek, modern toast notification smoothly slides onto your screen displaying the song title, artist, album, and high-resolution album art—all **without requiring Spotify Web API keys, developer tokens, or browser logins**.

It is engineered to be **zero-distraction**: it never steals mouse/keyboard focus while you are coding or gaming, uses under **3 MB of RAM** in standby, and automatically manages its own lifecycle alongside Spotify.

---

## ✨ Key Features

- 🔄 **Smart Lifecycle Sync**:
  - **Spotify Opens**: The app wakes up immediately, hooks into Windows GSMTC, and places its icon in the System Tray.
  - **Spotify Closes**: The app hides its tray icon, dismisses any active toast, and enters **Ultra-Low-Power Standby** (~2–3 MB RAM, 0% CPU).
  - **Spotify Reopens**: Wakes up instantly with zero manual intervention.
- 🚀 **Run at Windows Startup**: Automatically starts in standby mode with Windows via the Registry.
- 🔒 **Single Instance Mutex**: Prevents duplicate processes from running simultaneously.
- 🎮 **Zero Focus Stealing (`WS_EX_NOACTIVATE`)**:
  - The notification stays `Topmost` without interrupting your active window (games, IDEs, full-screen apps, or text editors).
- 🧹 **Zero Taskbar Clutter (`WS_EX_TOOLWINDOW`)**:
  - Lives exclusively in the Windows System Tray; never clutters your Taskbar or `Alt + Tab` switcher.
- 🎯 **Native WinRT GSMTC Listener**:
  - Accurately filters for Spotify's `SourceAppId`.
  - Extracts `Title`, `Artist`, `AlbumTitle`, and streams cover art directly into thread-safe frozen `BitmapImage` assets.
- ⏭️ **Interactive Skip Button**:
  - Click the **Next (Skip)** button right on the toast to fast-forward through your playlist using `TrySkipNextAsync()`.
- ⏸️ **Hover to Pause Timer**:
  - Hovering your mouse over the toast freezes the auto-dismiss timer so you can read long titles or click controls.
- 🎨 **Fluid Animations**:
  - Smooth slide-in, fade-in, slide-out, and fade-out transitions powered by `CubicEase` acceleration.
- 🛠️ **Live Configuration (`config.json`)**:
  - Customize screen alignment, margins, dimensions, display duration, and colors on-the-fly without rebuilding.
- 📌 **System Tray Context Menu**:
  - **Reload Config**: Reload settings instantly.
  - **Test Toast**: Trigger a preview notification.
  - **Auto Sync with Spotify**: Toggle smart standby.
  - **Start with Windows**: Toggle startup registration.
  - **Exit**: Cleanly unregister hooks and terminate.

---

## 🖥️ System Requirements

- **Operating System**: Windows 10 (Build 19041 / version 2004 or later) or Windows 11.
- **Runtime / SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or .NET 8.0 Desktop Runtime if running pre-built binaries).
- **Player**: [Spotify Desktop Client for Windows](https://www.spotify.com/download/windows/).

---

## 🚀 Quick Start & Building

### 1. Clone the repository
```bash
git clone https://github.com/your-username/SpotifyNowPlaying.git
cd SpotifyNowPlaying
```

### 2. Build the project
```powershell
# Build in Release mode
dotnet build -c Release
```

### 3. Run the application
```powershell
dotnet run -c Release
```
> The application will start silently in the background. Look for the green Spotify icon in your **Windows System Tray** (bottom-right near the clock).

### 4. Publish as a Single Executable (Optional)
To generate a standalone `.exe` that you can run on any Windows machine without installing .NET:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

## 💡 Recommended: Disable Spotify's Default Toast

To avoid overlapping duplicate popups (Spotify's built-in banner vs. this custom overlay):

1. Open **Spotify Desktop**.
2. Click your **Profile picture** $\rightarrow$ **Settings** (or press `Ctrl + P`).
3. Scroll down to the **Display** section.
4. Toggle **"Show desktop notifications when the song changes"** to **OFF**.

---

## ⚙️ Configuration (`config.json`)

The `config.json` file is automatically copied to your output folder alongside the `.exe`:

```json
{
  "Position": {
    "Alignment": "TopRight",
    "OffsetX": 20,
    "OffsetY": 20,
    "CustomX": 0,
    "CustomY": 0
  },
  "DisplayDurationMs": 4000,
  "AnimationDurationMs": 300,
  "Style": {
    "Width": 340,
    "Height": 90,
    "CornerRadius": 12,
    "BackgroundColor": "#E6181818",
    "TextColor": "#FFFFFF",
    "AccentColor": "#1DB954"
  },
  "AutoLifeCycle": {
    "AutoSyncWithSpotify": true,
    "StartWithWindows": true,
    "CheckIntervalMs": 1500
  }
}
```

### Settings Reference

| Key | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Position.Alignment` | string | `"TopRight"` | Screen position: `"TopRight"`, `"TopCenter"`, `"TopLeft"`, `"BottomRight"`, `"BottomLeft"`, or `"Custom"`. |
| `Position.OffsetX` | number | `20` | Horizontal margin from screen edge in pixels (auto-adjusts for taskbar). |
| `Position.OffsetY` | number | `20` | Vertical margin from screen edge in pixels. |
| `Position.CustomX` / `CustomY` | number | `0` | Absolute pixel coordinates used only when `Alignment` is `"Custom"`. |
| `DisplayDurationMs` | number | `4000` | Duration (in milliseconds) the toast remains visible before auto-dismissing. |
| `AnimationDurationMs` | number | `300` | Duration (in milliseconds) of slide/fade transitions. |
| `Style.Width` | number | `340` | Width of the toast flyout in pixels. |
| `Style.Height` | number | `90` | Height of the toast flyout in pixels. |
| `Style.CornerRadius` | number | `12` | Corner rounding radius in pixels. |
| `Style.BackgroundColor` | string | `"#E6181818"` | 8-digit HEX code (`#AARRGGBB`) supporting alpha transparency. |
| `Style.TextColor` | string | `"#FFFFFF"` | HEX color code for primary track and artist text. |
| `Style.AccentColor` | string | `"#1DB954"` | HEX color code for accents and buttons (Spotify Green). |
| `AutoLifeCycle.AutoSyncWithSpotify` | bool | `true` | Automatically activate/standby according to Spotify process status. |
| `AutoLifeCycle.StartWithWindows` | bool | `true` | Register in Windows Registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. |
| `AutoLifeCycle.CheckIntervalMs` | number | `1500` | Interval in ms for checking Spotify process state. |

> 💡 **Tip**: After editing `config.json`, right-click the Tray Icon and choose **"Reload Config"** to apply changes immediately without restarting!

---

## 📂 Project Architecture

```text
SpotifyNowPlaying/
├── Models/
│   ├── AppConfig.cs               # Strongly-typed schema for config.json
│   └── TrackInfo.cs               # Song metadata model & frozen BitmapImage cover art
├── Services/
│   ├── ConfigService.cs           # Reads, validates, and reloads config.json
│   ├── SpotifyMediaService.cs     # WinRT GSMTC integration & playback controls
│   ├── SpotifyLifecycleService.cs # Spotify process observer & standby memory optimizer
│   └── StartupService.cs          # Registry helper for Windows startup management
├── MainWindow.xaml                # XAML UI template for the custom toast flyout
├── MainWindow.xaml.cs             # Window styling (Win32 interop), animations & timers
├── App.xaml                       # Application entry definition (Explicit shutdown mode)
├── App.xaml.cs                    # App lifecycle, System Tray NotifyIcon, and Mutex
├── config.json                    # User-editable runtime configuration
├── SpotifyNowPlaying.csproj       # .NET 8 WPF project file targeting Windows SDK WinRT
└── README.md                      # Documentation (English & Tiếng Việt)
```

---
---

<a name="tiếng-việt"></a>
# 🇻🇳 Tiếng Việt

## 📖 Giới thiệu

**Spotify Now Playing Toast Overlay** là ứng dụng chạy ngầm siêu nhẹ dành cho Windows 10 & 11, được xây dựng trên nền tảng **C# .NET 8 WPF** và API hệ thống **WinRT GSMTC (Global System Media Transport Controls)**.

Mỗi khi Spotify chuyển bài hát, một bảng thông báo nhỏ gọn với thiết kế hiện đại sẽ trượt mượt mà lên màn hình hiển thị tên bài, ca sĩ, album và ảnh bìa chất lượng cao—**hoàn toàn không cần tạo Spotify API Token, Client Secret hay đăng nhập tài khoản**.

Ứng dụng được thiết kế tối ưu với triết lý **không gây phiền nhiễu**: không cướp tiêu điểm chuột/bàn phím khi bạn đang gõ văn bản, lập trình hoặc chơi game, tiêu thụ chỉ khoảng **2–3 MB RAM** ở chế độ chờ và tự động quản lý vòng đời theo Spotify.

---

## ✨ Tính năng nổi bật

- 🔄 **Đồng bộ vòng đời thông minh (Smart Lifecycle Sync)**:
  - **Khi mở Spotify**: Ứng dụng tự động kích hoạt ngay lập tức, kết nối Windows GSMTC và hiển thị biểu tượng ở khay hệ thống.
  - **Khi tắt Spotify**: Tự động đóng thông báo, ẩn khay hệ thống và chuyển sang chế độ **Ngủ ngầm (Smart Standby)**, giải phóng bộ nhớ RAM tối đa (chỉ còn khoảng 2–3 MB RAM, 0% CPU).
  - **Khi mở lại Spotify**: Ngay lập tức thức dậy và hoạt động bình thường mà không cần bất kỳ thao tác thủ công nào.
- 🚀 **Khởi động cùng Windows (Run at Startup)**: Tự động chạy ngầm ở chế độ chờ cùng Windows qua Windows Registry (`HKCU\...\Run`).
- 🔒 **Đảm bảo duy nhất một tiến trình (Single Instance Mutex)**: Ngăn ngừa việc mở trùng lặp nhiều app.
- 🎮 **Không cướp quyền điều khiển (`WS_EX_NOACTIVATE`)**:
  - Thông báo hiển thị đè lên trên cùng (`Topmost`) nhưng hoàn toàn không làm mất tiêu điểm (focus) cửa sổ bạn đang làm việc (hỗ trợ tốt khi đang gõ văn bản, code hoặc chơi game full-screen).
- 🧹 **Không làm bẩn Taskbar (`WS_EX_TOOLWINDOW`)**:
  - Thường trú tại khay hệ thống (**System Tray**), không xuất hiện trên Taskbar hay bảng chuyển ứng dụng `Alt + Tab`.
- 🎯 **Lắng nghe GSMTC chuẩn xác**:
  - Lọc chuẩn xác phiên phát nhạc của Spotify qua `SourceAppId`.
  - Trích xuất tự động `Title`, `Artist`, `AlbumTitle` và giải mã luồng ảnh bìa (`Thumbnail` stream) sang `BitmapImage` với cơ chế freeze thread-safe.
- ⏭️ **Nút chuyển bài trực tiếp (Skip Button)**:
  - Bấm nút **Next (Skip)** ngay trên thông báo để chuyển bài hát lập tức thông qua hàm `TrySkipNextAsync()`.
- ⏸️ **Rê chuột giữ thông báo (Hover to Pause)**:
  - Khi rê chuột vào thông báo, ứng dụng tự động dừng đếm ngược để người dùng kịp đọc hoặc click điều khiển.
- 🎨 **Hiệu ứng chuyển động mượt mà**:
  - Hiệu ứng Slide-in, Fade-in khi xuất hiện và Slide-out, Fade-out khi biến mất với gia tốc chuyển động mềm mại (`CubicEase`).
- 🛠️ **Tùy biến linh hoạt qua `config.json`**:
  - Dễ dàng chỉnh sửa vị trí tọa độ, màu sắc, bo góc, kích thước, thời gian hiển thị mà không cần build lại mã nguồn.
- 📌 **Menu khay hệ thống (System Tray)** tiện ích:
  - **Reload Config**: Nạp lại cài đặt tức thì.
  - **Test Toast**: Bắn thử một thông báo mẫu để xem trước giao diện và vị trí.
  - **Tự động đồng bộ theo Spotify**: Bật/tắt chế độ tự kích hoạt theo Spotify.
  - **Khởi động cùng Windows**: Bật/tắt tự động khởi động cùng Windows.
  - **Exit**: Thoát ứng dụng an toàn và giải phóng tài nguyên.

---

## 🖥️ Yêu cầu hệ thống

- **Hệ điều hành**: Windows 10 (bản build 19041 / version 2004 trở lên) hoặc Windows 11.
- **Môi trường SDK/Runtime**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (hoặc .NET 8.0 Desktop Runtime nếu chạy file build sẵn).
- **Ứng dụng phát nhạc**: [Spotify Desktop cho Windows](https://www.spotify.com/download/windows/).

---

## 🚀 Hướng dẫn Build & Chạy

### 1. Clone repository
```bash
git clone https://github.com/your-username/SpotifyNowPlaying.git
cd SpotifyNowPlaying
```

### 2. Build ứng dụng
```powershell
dotnet build -c Release
```

### 3. Khởi chạy ứng dụng
```powershell
dotnet run -c Release
```
> Ứng dụng sẽ chạy ngầm ngay lập tức. Biểu tượng Spotify màu xanh sẽ xuất hiện tại **Khay hệ thống (System Tray)** ở góc dưới bên phải màn hình.

### 4. Đóng gói thành 1 file .exe duy nhất (Tùy chọn)
Nếu bạn muốn tạo file chạy độc lập không yêu cầu máy cài sẵn .NET Runtime:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

## 💡 Khuyến nghị: Tắt thông báo mặc định của Spotify

Để tránh việc màn hình hiển thị trùng lặp 2 thông báo cùng lúc (1 thông báo mặc định của Spotify và 1 thông báo custom này):

1. Mở ứng dụng **Spotify Desktop**.
2. Nhấp vào **Ảnh đại diện** ở góc trên bên phải $\rightarrow$ Chọn **Cài đặt (Settings)** (hoặc dùng phím tắt `Ctrl + P`).
3. Cuộn trang cài đặt xuống phần **Hiển thị (Display)**.
4. Tìm mục: **"Hiển thị thông báo trên màn hình khi đổi bài hát" (Show desktop notifications when the song changes)**.
5. Gạt công tắc sang trạng thái **Tắt (Off)**.

---

## ⚙️ Hướng dẫn Cấu hình `config.json`

File `config.json` nằm cùng thư mục với file `.exe` cho phép bạn tùy chỉnh mọi thông số:

```json
{
  "Position": {
    "Alignment": "TopRight",
    "OffsetX": 20,
    "OffsetY": 20,
    "CustomX": 0,
    "CustomY": 0
  },
  "DisplayDurationMs": 4000,
  "AnimationDurationMs": 300,
  "Style": {
    "Width": 340,
    "Height": 90,
    "CornerRadius": 12,
    "BackgroundColor": "#E6181818",
    "TextColor": "#FFFFFF",
    "AccentColor": "#1DB954"
  },
  "AutoLifeCycle": {
    "AutoSyncWithSpotify": true,
    "StartWithWindows": true,
    "CheckIntervalMs": 1500
  }
}
```

### Bảng giải thích chi tiết:

| Tham số | Kiểu | Mặc định | Ý nghĩa & Tùy chọn |
| :--- | :--- | :--- | :--- |
| `Position.Alignment` | string | `"TopRight"` | Vị trí xuất hiện: `"TopRight"`, `"TopCenter"`, `"TopLeft"`, `"BottomRight"`, `"BottomLeft"`, hoặc `"Custom"`. |
| `Position.OffsetX` | number | `20` | Khoảng cách lề ngang (pixel, tự động né thanh Taskbar). |
| `Position.OffsetY` | number | `20` | Khoảng cách lề dọc (pixel). |
| `Position.CustomX` / `CustomY` | number | `0` | Tọa độ điểm xuất hiện chính xác khi `Alignment` là `"Custom"`. |
| `DisplayDurationMs` | number | `4000` | Thời gian thông báo hiển thị trước khi ẩn (mili-giây, 4000 = 4s). |
| `AnimationDurationMs` | number | `300` | Tốc độ hiệu ứng xuất hiện / biến mất (mili-giây, 300 = 0.3s). |
| `Style.Width` | number | `340` | Chiều rộng khung thông báo (pixel). |
| `Style.Height` | number | `90` | Chiều cao khung thông báo (pixel). |
| `Style.CornerRadius` | number | `12` | Bán kính bo tròn góc khung thông báo (pixel). |
| `Style.BackgroundColor` | string | `"#E6181818"` | Mã màu nền HEX 8 ký tự (`#AARRGGBB`) hỗ trợ độ trong suốt. |
| `Style.TextColor` | string | `"#FFFFFF"` | Mã màu HEX của tên bài hát và ca sĩ. |
| `Style.AccentColor` | string | `"#1DB954"` | Mã màu HEX điểm nhấn và nút bấm (Xanh Spotify). |
| `AutoLifeCycle.AutoSyncWithSpotify` | bool | `true` | Bật/tắt tự động ngủ khi tắt Spotify và thức dậy khi mở Spotify. |
| `AutoLifeCycle.StartWithWindows` | bool | `true` | Đăng ký tự khởi động cùng Windows qua Registry. |
| `AutoLifeCycle.CheckIntervalMs` | number | `1500` | Chu kỳ quét tiến trình Spotify (mili-giây). |

> 💡 **Mẹo**: Sau khi sửa `config.json`, chỉ cần nhấp chuột phải vào biểu tượng ứng dụng ở System Tray và chọn **"Reload Config"** để nạp cài đặt mới ngay lập tức mà không cần khởi động lại app!

---

## 📂 Cấu trúc thư mục dự án

```text
SpotifyNowPlaying/
├── Models/
│   ├── AppConfig.cs               # Cấu trúc dữ liệu ánh xạ file config.json
│   └── TrackInfo.cs               # Dữ liệu bài hát & ảnh bìa BitmapImage
├── Services/
│   ├── ConfigService.cs           # Đọc, lưu và nạp lại cấu hình config.json
│   ├── SpotifyMediaService.cs     # Tương tác WinRT GSMTC & điều khiển Spotify
│   ├── SpotifyLifecycleService.cs # Giám sát tiến trình Spotify & thu hồi RAM khi ngủ
│   └── StartupService.cs          # Quản lý đăng ký khởi động cùng Windows qua Registry
├── MainWindow.xaml                # Giao diện XAML của Toast Flyout
├── MainWindow.xaml.cs             # Logic animations, Win32 style & timer
├── App.xaml                       # Khai báo ứng dụng ngầm (OnExplicitShutdown)
├── App.xaml.cs                    # Quản lý vòng đời, System Tray NotifyIcon, Mutex
├── config.json                    # File cấu hình tùy biến
├── SpotifyNowPlaying.csproj       # File cấu hình .NET 8 WPF & Windows SDK WinRT
└── README.md                      # Tài liệu hướng dẫn (Song ngữ Anh - Việt)
```

---

## 📄 Giấy phép (License)

Dự án được phân phối dưới giấy phép [MIT License](LICENSE). Bạn có thể tự do sử dụng, chỉnh sửa và phân phối theo nhu cầu cá nhân.
