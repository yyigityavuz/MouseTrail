# Mouse Trail

A lightweight Windows desktop app that draws a customizable, fading trail behind your mouse cursor. Built with C# and WPF, it runs quietly in the system tray and never gets in the way of your clicks.

<!-- Add a screenshot or GIF here: ![Mouse Trail demo](docs/demo.gif) -->

## Features
- **Click-through overlay:** The trail window ignores the mouse, so you can keep using your desktop and other apps normally.
- **Multi-monitor support:** One overlay spans all monitors (including monitors left of or above the primary one) and handles mixed DPI scaling and display changes.
- **Customizable:** Change color, thickness and length from the tray menu. Settings are saved automatically.
- **Start with Windows:** Optional autostart toggle in the tray menu.
- **Lightweight:** Object-pooled line segments (no per-frame allocations), refresh-rate-independent fading, and no work while the mouse is idle.
- **Single instance:** Launching it twice won't give you two trails.

## Download
Grab the latest `MouseTrail-*-win-x64.zip` from the [Releases](../../releases) page, extract it, and run `MouseTrail.exe`. It is self-contained, so you don't need to install .NET.

> The executable is not code-signed, so Windows SmartScreen may warn you the first time. Choose **More info → Run anyway**, or build it yourself from source (see below).

## Usage
The app hides in the system tray (notification area). Right-click the tray icon to:
- **Select Color** – pick the trail color (the tray icon follows it)
- **Set Thickness** / **Set Length** – choose a preset
- **Start with Windows** – toggle autostart
- **Exit** – close the app

Settings are stored in `%AppData%\MouseTrail\settings.json`.

## Requirements
- Windows 10 or 11 (x64)

## Build from source
Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
git clone https://github.com/yyigityavuz/MouseTrail.git
cd MouseTrail
dotnet run --project MouseTrail/MouseTrail.csproj
```

Create a self-contained single-file build:

```powershell
dotnet publish MouseTrail/MouseTrail.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Releasing
Pushing a tag such as `v1.0.0` runs the GitHub Actions workflow, which builds, publishes and attaches `MouseTrail-v1.0.0-win-x64.zip` to a new GitHub release.

## Technologies
- C# / .NET 10
- WPF (Windows Presentation Foundation) and Windows Forms (tray icon, color dialog)
- Windows API (P/Invoke) for cursor tracking, layered/click-through windows and multi-monitor placement

## License
[MIT](LICENSE)
