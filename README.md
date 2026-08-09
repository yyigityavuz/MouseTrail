# Mouse Trail

A lightweight, performance-optimized Windows desktop application that creates a customizable visual trail behind your mouse cursor. Built with C# and WPF, it runs seamlessly in the background without interfering with your daily tasks.

## Features
- **Click-Through Overlay:** The trail window is completely transparent to mouse clicks. You can interact with your desktop and other applications without interruption.
- **Performance Optimized:** Uses the Object Pool pattern to manage line segments, eliminating constant instantiation and garbage collection overhead.
- **Customizable:** Change the trail color, thickness, and length dynamically via the system tray.
- **Unobtrusive:** Runs entirely in the background with a minimal footprint, accessible only through the system tray.

## Technologies Used
- C# / .NET 10
- WPF (Windows Presentation Foundation)
- Windows API (P/Invoke) for global cursor tracking and layered windows

## Usage
Once started, the application hides in the system tray (notification area). 
- **Right-click** the tray icon to open the configuration menu.
- Use the menu to change colors, thickness, and length.
- Click **Exit** to close the application.