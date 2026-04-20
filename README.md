# TodoApp

Ứng dụng quản lý công việc cá nhân đầy đủ tính năng, xây dựng trên **WPF / .NET 8**, giao diện Material Design hiện đại, hỗ trợ đầy đủ tiếng Việt và tiếng Anh.

---

## Mục lục

- [Tính năng](#tính-năng)
- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Cài đặt & Chạy từ source](#cài-đặt--chạy-từ-source)
- [Publish ra file .exe](#publish-ra-file-exe)
- [Cấu trúc project](#cấu-trúc-project)
- [Hướng dẫn sử dụng](#hướng-dẫn-sử-dụng)
- [Cấu hình nâng cao](#cấu-hình-nâng-cao)
- [Tech Stack](#tech-stack)
- [Dependencies](#dependencies)

---

## Tính năng

### Quản lý công việc (Task Management)
- ✅ Tạo, sửa, xóa công việc với đầy đủ thông tin: tiêu đề, mô tả (Markdown), độ ưu tiên, trạng thái, deadline
- ✅ 4 mức độ ưu tiên: **Low / Medium / High / Critical** — màu sắc phân biệt rõ ràng
- ✅ 4 trạng thái: **Todo / In Progress / Done / Archived**
- ✅ Ghim công việc quan trọng lên đầu danh sách
- ✅ Subtasks (công việc con) với progress bar theo dõi tiến độ
- ✅ Tags và Labels để phân loại
- ✅ Tìm kiếm và lọc theo nhiều tiêu chí (Hôm nay, Trễ hạn, Đã ghim, Ưu tiên cao, Đã xong)
- ✅ Sắp xếp theo Ưu tiên / Hạn chót / Ngày tạo / Tên
- ✅ Drag & Drop để sắp xếp thứ tự

### Công việc lặp lại (Recurring Tasks)
- ✅ Lịch lặp: **Hàng ngày / Hàng tuần / Hàng tháng / Tùy chỉnh** (theo số ngày)
- ✅ Chọn ngày trong tuần cho lịch weekly
- ✅ Ngày kết thúc tùy chọn
- ✅ Tự động sinh công việc mới khi đến kỳ (background service)

### Kanban Board
- ✅ Giao diện Kanban với 3 cột: **To Do / In Progress / Done**
- ✅ Drag & Drop card giữa các cột
- ✅ Phát âm thanh khi hoàn thành task

### Calendar View
- ✅ Xem công việc theo lịch tháng
- ✅ Highlight ngày có task, ngày trễ hạn
- ✅ Click vào ngày để xem task của ngày đó

### Thống kê (Statistics)
- ✅ Dashboard tổng quan: tổng task, hoàn thành, đang làm, trễ hạn
- ✅ Biểu đồ hoàn thành theo ngày/tuần/tháng (OxyPlot)
- ✅ Phân bố theo độ ưu tiên
- ✅ Tỷ lệ hoàn thành tổng thể

### Hệ thống thông báo (Notifications)
- ✅ Toast notification tích hợp sẵn (không phụ thuộc Windows notification center)
- ✅ 5 mốc thông báo per task (có thể kết hợp):
  - 1 ngày trước deadline
  - 1 giờ trước deadline
  - 5 phút trước deadline
  - Đúng deadline
  - 1 ngày sau deadline (nhắc trễ hạn)
- ✅ Background service kiểm tra định kỳ (interval cấu hình được)
- ✅ Phát hiện thông báo bị bỏ lỡ khi khởi động lại app
- ✅ Gộp nhóm thông báo gần nhau trong cùng khoảng thời gian

### Âm thanh (Sound)
- ✅ Âm thanh thông báo riêng theo từng độ ưu tiên (mp3 / wav)
- ✅ Âm thanh khi hoàn thành task
- ✅ File âm thanh tùy chỉnh (browse & chọn)
- ✅ Điều chỉnh âm lượng master (0–100%)
- ✅ Fallback sang Windows system sounds nếu không có file
- ✅ WASAPI audio engine — low-latency, không bị cắt âm đầu file
- ✅ Nút **"Thử âm thanh"** preview ngay trong cài đặt

### Thông báo Email
- ✅ Tích hợp **Brevo API** (khuyến nghị, miễn phí) hoặc **SMTP** tùy chỉnh
- ✅ Gửi email nhắc nhở theo các mốc thời gian đã chọn
- ✅ Cấu hình tên/email người gửi và người nhận

### Ghi chú nhanh (Sticky Notes)
- ✅ Tạo sticky note với màu sắc tùy chỉnh
- ✅ Cửa sổ sticky note riêng biệt, always-on-top

### Watch Later
- ✅ Danh sách lưu link / nội dung để xem sau

### Goals (Mục tiêu)
- ✅ Quản lý mục tiêu dài hạn
- ✅ Liên kết task với goal để theo dõi tiến độ

### Chế độ Focus (Pomodoro-style)
- ✅ Timer tùy chọn: 15 / 25 / 45 / 60 / 90 / 120 phút
- ✅ Tắt toast notification và âm thanh trong khi focus
- ✅ Phát âm thanh báo hiệu kết thúc session

### Giao diện & Chủ đề (Themes)
- ✅ **Light / Dark mode** + tự động chuyển theo giờ (cấu hình giờ bật/tắt)
- ✅ 3 preset màu: **Pink / Purple / Blue** + Custom (chọn màu hex tùy ý)
- ✅ Animation mượt mà + hiệu ứng confetti khi hoàn thành task (bật/tắt)
- ✅ Hỗ trợ **Tiếng Việt / English** — chuyển ngôn ngữ không cần restart

### Hệ thống & Tiện ích
- ✅ **System Tray**: thu nhỏ xuống khay hệ thống, app chạy ngầm
- ✅ **Global Hotkey**: mặc định `Ctrl+Alt+T` — mở app từ bất kỳ đâu (cấu hình được)
- ✅ **Quick Add**: cửa sổ thêm task nhanh không cần mở app chính
- ✅ Khởi động cùng Windows (tùy chọn)
- ✅ Backup tự động theo lịch + backup khi thoát app
- ✅ Import / Export dữ liệu (JSON)
- ✅ Restore từ file backup
- ✅ Dữ liệu lưu tại `%AppData%\TodoApp\`

---

## Yêu cầu hệ thống

| Yêu cầu | Chi tiết |
|---------|---------|
| OS | Windows 10 / 11 (x64) |
| .NET Runtime | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) *(chỉ cần khi chạy từ source)* |
| .NET SDK | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) *(chỉ cần khi build từ source)* |
| RAM | Tối thiểu 256 MB |
| Ổ cứng | ~150 MB (bản self-contained publish) |

> **Bản publish self-contained**: Không cần cài .NET Runtime — đã gộp sẵn vào `.exe`.

---

## Cài đặt & Chạy từ source

### 1. Clone repository

```bash
git clone https://github.com/<your-username>/TodoApp.git
cd TodoApp
```

### 2. Restore packages

```bash
dotnet restore TodoApp/TodoApp.csproj
```

### 3. Build

```bash
dotnet build TodoApp/TodoApp.csproj --configuration Debug
```

### 4. Chạy

```bash
dotnet run --project TodoApp/TodoApp.csproj --configuration Debug
```

---

## Publish ra file .exe

Tạo bản **single-file self-contained** (không cần cài .NET Runtime trên máy chạy):

```bash
dotnet publish TodoApp/TodoApp.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  --output ./publish
```

Output trong thư mục `./publish/`:

```
publish/
├── TodoApp.exe                  ← File thực thi chính (~75 MB)
├── D3DCompiler_47_cor3.dll      ← DirectX shader compiler (WPF bắt buộc)
├── PresentationNative_cor3.dll  ← WPF native rendering engine
├── wpfgfx_cor3.dll              ← WPF graphics pipeline
├── PenImc_cor3.dll              ← Pen/Tablet input
├── vcruntime140_cor3.dll        ← Visual C++ runtime
└── appsettings.json             ← Cấu hình logging
```

> ⚠️ **Copy toàn bộ thư mục `publish/`** — 5 file `.dll` là native dependencies bắt buộc của WPF, không thể bundle vào `.exe`. Chạy `TodoApp.exe` là xong.

---

## Cấu trúc project

```
TodoApp/
├── Assets/
│   ├── Icons/                  # App icons
│   └── Sounds/                 # Thư mục chứa file âm thanh (.wav / .mp3)
├── Behaviors/                  # WPF Attached Behaviors (drag-drop reorder)
├── Controls/                   # Custom controls (MarkdownTextBlock, ...)
├── Converters/                 # XAML Value Converters
├── Core/
│   ├── Enums/                  # Priority, TodoStatus, RecurrenceType, NotificationTiming
│   └── Models/                 # Domain models: TodoItem, Label, Goal, StickyNote, ...
│       └── Settings/           # AppSettings, SoundSettings, NotificationSettings, ...
├── Data/                       # Data layer: JSON repositories, AppDataStore (singleton cache)
├── Infrastructure/             # SystemTrayService, GlobalHotkeyService
├── Languages/
│   ├── vi.xaml                 # Tiếng Việt
│   └── en.xaml                 # English
├── Services/                   # Business logic
│   ├── TodoService.cs          # CRUD công việc
│   ├── NotificationService.cs  # Background notification polling (IHostedService)
│   ├── SoundService.cs         # WASAPI audio playback (NAudio)
│   ├── EmailService.cs         # Brevo API / SMTP
│   ├── AIService.cs            # Google Gemini integration
│   ├── BackupService.cs        # Backup / restore / export
│   ├── AutoBackupService.cs    # Auto backup (IHostedService)
│   ├── RecurringTaskService.cs # Recurring task generator (IHostedService)
│   ├── ThemeService.cs         # Runtime theme switching
│   ├── LocalizationService.cs  # Runtime language switching
│   └── ...                     # Goal, Label, StickyNote, WatchLater, Statistics, Toast
├── Themes/
│   ├── Colors.xaml             # Color palette & brushes
│   ├── Typography.xaml         # Font styles
│   ├── ControlStyles.xaml      # Button, CheckBox, Card styles
│   └── Animations.xaml         # Storyboard animations
├── ViewModels/                 # MVVM ViewModels (CommunityToolkit.Mvvm)
│   └── Base/ViewModelBase.cs   # Base class: IsBusy, RunBusyAsync, InitializeAsync
├── Views/                      # XAML Views & Dialogs
│   ├── MainWindow.xaml         # Shell window + sidebar navigation
│   ├── TaskListView.xaml       # Danh sách việc chính
│   ├── KanbanView.xaml         # Kanban board
│   ├── CalendarView.xaml       # Calendar
│   ├── StatisticsView.xaml     # Dashboard thống kê
│   ├── SettingsView.xaml       # Cài đặt (tabbed)
│   ├── StickyNotesView.xaml    # Sticky notes
│   ├── GoalView.xaml           # Mục tiêu
│   ├── LabelView.xaml          # Quản lý labels
│   ├── WatchLaterView.xaml     # Watch Later
│   ├── TaskDetailDialog.xaml   # Dialog tạo/sửa task
│   ├── ConfirmDeleteDialog.xaml # Dialog xác nhận xóa
│   ├── ToastWindow.xaml        # Toast notification popup
│   ├── QuickAddWindow.xaml     # Cửa sổ thêm task nhanh
│   └── StickyNoteWindow.xaml   # Cửa sổ sticky note riêng
├── App.xaml                    # Application resources, theme merge dicts
├── App.xaml.cs                 # Startup, DI container registration
├── Program.cs                  # Custom Main() entry point
└── TodoApp.csproj
```

---

## Hướng dẫn sử dụng

### Thêm task mới
- Nhấn **"+ Thêm việc"** trên header, **hoặc**
- Dùng global hotkey **`Ctrl+Alt+T`** từ bất kỳ đâu, **hoặc**
- Double-click vào task hiện có để chỉnh sửa

### Hoàn thành task
Click vào **vòng tròn checkbox** bên trái — task gạch ngang, phát âm thanh hoàn thành.

### Thao tác nhanh trên task card
Di chuột vào card để hiện 3 nút action:

| Nút | Chức năng |
|-----|-----------|
| ✏️ Bút chì | Mở form chỉnh sửa |
| 🔖 Bookmark | Ghim / Bỏ ghim lên đầu danh sách |
| 🗑️ Thùng rác | Xóa task (hiện confirm dialog có theme) |

> Double-click vào bất kỳ đâu trên card để mở nhanh form chỉnh sửa.

### Lọc & Sắp xếp
**Chip filter** phía trên danh sách:

| Filter | Hiển thị |
|--------|---------|
| Tất cả | Mọi task chưa xóa |
| Hôm nay | Deadline là hôm nay |
| Trễ hạn | Đã qua deadline chưa xong |
| Đã ghim | Task được pin |
| Cao+ | Ưu tiên High và Critical |
| Đã xong | Trạng thái Done |

**Sort** theo: Ưu tiên / Hạn chót / Ngày tạo / Tên — click lại để đảo chiều.

### Kanban Board
Chuyển sang tab **Kanban** — kéo thả card giữa các cột `To Do → In Progress → Done`.

### Cài đặt âm thanh
**Cài đặt → Âm thanh**:
1. Bật/tắt âm thanh tổng
2. Kéo slider điều chỉnh âm lượng
3. Bật "Phát âm thanh khi hoàn thành task"
4. Browse chọn file âm thanh tùy chỉnh (`.wav` hoặc `.mp3`)
5. Nhấn **"Thử âm thanh"** để nghe preview ngay

### Thông báo Email (Brevo)
**Cài đặt → Email**:
1. Bật email notifications
2. Chọn provider **Brevo** (miễn phí tới 300 email/ngày)
3. Nhập Brevo API Key (đăng ký tại [brevo.com](https://brevo.com))
4. Điền email gửi và nhận
5. Chọn các mốc muốn gửi email

### AI Assistant (Gemini)
**Cài đặt → AI**:
1. Nhập Google Gemini API Key (lấy miễn phí tại [aistudio.google.com](https://aistudio.google.com))
2. Chọn model (mặc định: `gemini-2.0-flash`)
3. Nhấn **"Kiểm tra"** để test kết nối
4. Khi tạo/sửa task, AI tự gợi ý tags phù hợp

### Backup & Restore
**Cài đặt → Backup**:
- **Backup ngay**: tạo file backup thủ công
- **Tự động backup**: cấu hình interval (theo giờ) và số lượng file tối đa giữ lại
- **Backup khi thoát**: bật để luôn có bản backup mới nhất
- **Restore**: chọn file `.json` backup để khôi phục

### Focus Mode
Nhấn **Focus** trên sidebar:
1. Chọn thời gian (15/25/45/60/90/120 phút)
2. Bắt đầu — toasts và âm thanh bị tắt tự động
3. Hết giờ: phát âm thanh báo hiệu

### System Tray & Global Hotkey
- Thu nhỏ cửa sổ → app chạy ngầm, icon hiện ở system tray
- Click icon tray → mở lại
- Bất kỳ đâu: `Ctrl+Alt+T` → focus app (hotkey cấu hình được trong Settings)

---

## Cấu hình nâng cao

### Ngôn ngữ
**Cài đặt → Chung → Ngôn ngữ**: Tiếng Việt / English. Áp dụng ngay, không cần restart.

### Theme & Màu sắc
**Cài đặt → Giao diện**:
- **Mode**: Light / Dark / Tự động (theo giờ cài đặt)
- **Preset**: Pink / Purple / Blue / Custom
- **Custom**: nhập mã hex màu primary và background
- Bật/tắt animations, confetti effect

### Global Hotkey
**Cài đặt → Chung → Phím tắt toàn cục**: nhập tổ hợp phím mong muốn.

### Dữ liệu lưu trữ
Mặc định tại `%AppData%\TodoApp\`:
```
%AppData%\TodoApp\
├── todos.json          # Danh sách công việc
├── settings.json       # Toàn bộ cài đặt app
├── labels.json         # Labels
├── goals.json          # Mục tiêu
├── stickynotes.json    # Sticky notes
├── watchlater.json     # Watch Later
└── Backups/            # File backup tự động & thủ công
```

---

## Tech Stack

| Thành phần | Công nghệ |
|-----------|-----------|
| Framework | .NET 8.0 WPF (Windows Presentation Foundation) |
| Language | C# 12 |
| Architecture | MVVM — CommunityToolkit.Mvvm 8.2 (source generators) |
| UI Components | Material Design In XAML 5.1 |
| Audio Engine | NAudio 2.2.1 — WASAPI shared mode |
| Charts | OxyPlot.Wpf 2.1 |
| DI / Background Services | Microsoft.Extensions.Hosting 8.0 |
| System Tray | Hardcodet.NotifyIcon.Wpf 1.1 |
| Global Hotkey | NHotkey.Wpf 3.0 |
| WPF Behaviors | Microsoft.Xaml.Behaviors.Wpf 1.1 |
| AI | Google Gemini REST API |
| Email | Brevo Transactional Email API / SMTP |
| Persistence | System.Text.Json (JSON flat-file) |
| Platform | Windows 10 / 11 — x64 |

---

## Dependencies

```xml
<PackageReference Include="CommunityToolkit.Mvvm"                   Version="8.2.2"  />
<PackageReference Include="Microsoft.Extensions.Hosting"             Version="8.0.1"  />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1"  />
<PackageReference Include="Microsoft.Extensions.Http"                Version="8.0.1"  />
<PackageReference Include="Microsoft.Extensions.Configuration.Json"  Version="8.0.1"  />
<PackageReference Include="Microsoft.Extensions.Logging"             Version="8.0.1"  />
<PackageReference Include="Microsoft.Extensions.Logging.Debug"       Version="8.0.1"  />
<PackageReference Include="NAudio"                                   Version="2.2.1"  />
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf"            Version="1.1.77" />
<PackageReference Include="MaterialDesignThemes"                     Version="5.1.0"  />
<PackageReference Include="MaterialDesignColors"                     Version="3.1.0"  />
<PackageReference Include="Hardcodet.NotifyIcon.Wpf"                Version="1.1.0"  />
<PackageReference Include="NHotkey.Wpf"                             Version="3.0.0"  />
<PackageReference Include="OxyPlot.Wpf"                             Version="2.1.0"  />
```

---

## License

MIT License — xem file [LICENSE](LICENSE) để biết thêm chi tiết.
