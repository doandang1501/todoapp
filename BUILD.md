# TodoApp — Build & Run Guide

## Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET 8 SDK | 8.0.x | https://dotnet.microsoft.com/download/dotnet/8 |
| Visual Studio 2022 | 17.8+ | (optional, for IDE) |

> **Note:** Only the .NET 8 *runtime* is currently installed on this machine.
> Install the **.NET 8 SDK** first — the runtime alone cannot build.

---

## Quick Start (CLI)

```powershell
# 1. Navigate to solution root
cd D:\Congty\canhan\ToDoApp

# 2. Restore NuGet packages
dotnet restore

# 3. Build (Debug)
dotnet build TodoApp/TodoApp.csproj

# 4. Run
dotnet run --project TodoApp/TodoApp.csproj
```

---

## Publish as Standalone .exe

```powershell
dotnet publish TodoApp/TodoApp.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -o ./publish
```

Output: `./publish/TodoApp.exe`  
User double-clicks `TodoApp.exe` — requires .NET 8 runtime on target machine.

---

## Before First Build — Add Asset Files

Two placeholder sound files are required (or the csproj Resource embed will warn).
Add them to `TodoApp/Assets/Sounds/`:

| File | Source |
|------|--------|
| `notification.wav` | Any short WAV ≤ 2 s. Free options: freesound.org |
| `complete.wav`     | Any pleasant short WAV. |

For the app icon, add `TodoApp/Assets/Icons/app.ico`.
A free .ico generator: https://icoconvert.com

---

## Data Location

All user data is stored in:
```
%AppData%\TodoApp\
  todos.json
  stickynotes.json
  settings.json
  Backups\
  Logs\
```

---

## Part Build Map

| Part | Status | Description |
|------|--------|-------------|
| **Part 1**  | ✅ Done | Architecture, models, data layer, theme |
| Part 2  | Pending | TodoService + StatisticsService          |
| Part 3  | Pending | Background services (scheduler, backup)  |
| Part 4  | Pending | System tray + global hotkey              |
| Part 5  | Pending | Task list UI (List view + filters)       |
| Part 6  | Pending | Task detail dialog (CRUD + subtasks)     |
| Part 7  | Pending | Kanban board with drag & drop            |
| Part 8  | Pending | Toast notifications + sound              |
| Part 9  | Pending | Email via Brevo REST API                 |
| Part 10 | Pending | Theme engine (pink/dark/custom)          |
| Part 11 | Pending | Animations (tick, confetti)              |
| Part 12 | Pending | Statistics + charts                      |
| Part 13 | Pending | Settings UI                              |
| Part 14 | Pending | Sticky notes + quick panel + focus mode  |
| Part 15 | Pending | Backup/restore + data cleanup            |
| Part 16 | Pending | Startup + final publish                  |
