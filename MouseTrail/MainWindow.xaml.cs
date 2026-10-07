using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace MouseTrail
{
    public partial class MainWindow : Window
    {
        // Object Pool pattern to avoid continuous instantiation and garbage collection overhead
        private Line[] linePool;
        private int poolIndex = 0;
        private const int MAX_LINES = 100; // Maximum allowed trail capacity

        private System.Windows.Point? lastMousePos = null;

        // --- CONFIGURATION VARIABLES ---
        private readonly TrailSettings settings = TrailSettings.Load();
        private SolidColorBrush trailColor;
        private double trailThickness;
        private int trailLength;

        private const double START_OPACITY = 0.9;
        // Seconds of fade time per unit of trail length (length 40 -> ~0.4s)
        private const double FADE_SECONDS_PER_LENGTH = 0.01;
        private readonly Stopwatch frameClock = Stopwatch.StartNew();
        private double lastFrameSeconds;
        private bool anyVisible; // lets the fade loop sleep while the mouse is idle

        private System.Windows.Forms.NotifyIcon trayIcon;
        private bool colorPickerOpen;

        // Virtual screen (all monitors) in physical pixels
        private int virtualLeft, virtualTop;
        private double dpiScaleX = 1, dpiScaleY = 1;

        public MainWindow()
        {
            InitializeComponent();
            trailColor = new SolidColorBrush(settings.ToColor());
            trailColor.Freeze();
            trailThickness = settings.Thickness;
            trailLength = Math.Clamp(settings.Length, 1, MAX_LINES);
            this.Loaded += OnLoaded;
            CompositionTarget.Rendering += OnRender;

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            SetupTrayIcon();
            InitializeObjectPool(); // Pre-instantiate the lines
        }

        private void InitializeObjectPool()
        {
            linePool = new Line[MAX_LINES];
            for (int i = 0; i < MAX_LINES; i++)
            {
                linePool[i] = new Line
                {
                    Opacity = 0, // Hidden by default
                    StrokeStartLineCap = PenLineCap.Flat,
                    StrokeEndLineCap = PenLineCap.Flat
                };
                TrailCanvas.Children.Add(linePool[i]); // Added to the UI once, never removed
            }
        }

        private void SetupTrayIcon()
        {
            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Icon = TrayIconFactory.Create(ToDrawingColor(trailColor.Color));
            trayIcon.Visible = true;
            trayIcon.Text = "Mouse Trail";

            var menu = new System.Windows.Forms.ContextMenuStrip();

            menu.Items.Add("Select Color", null, (s, e) => PickColor());

            var thicknessMenu = new System.Windows.Forms.ToolStripMenuItem("Set Thickness");
            thicknessMenu.DropDownItems.Add("Very Thin (6)", null, (s, e) => SetThickness(6));
            thicknessMenu.DropDownItems.Add("Normal (12)", null, (s, e) => SetThickness(12));
            thicknessMenu.DropDownItems.Add("Thick (24)", null, (s, e) => SetThickness(24));
            thicknessMenu.DropDownItems.Add("Very Thick (40)", null, (s, e) => SetThickness(40));
            menu.Items.Add(thicknessMenu);

            var lengthMenu = new System.Windows.Forms.ToolStripMenuItem("Set Length");
            lengthMenu.DropDownItems.Add("Short (20)", null, (s, e) => SetLength(20));
            lengthMenu.DropDownItems.Add("Normal (40)", null, (s, e) => SetLength(40));
            lengthMenu.DropDownItems.Add("Long (80)", null, (s, e) => SetLength(80));
            menu.Items.Add(lengthMenu);

            menu.Items.Add("-");
            menu.Items.Add("Exit", null, (s, e) => System.Windows.Application.Current.Shutdown());

            trayIcon.ContextMenuStrip = menu;
        }

        private void SetThickness(double value)
        {
            trailThickness = value;
            settings.Thickness = value;
            settings.Save();
        }

        private void SetLength(int value)
        {
            trailLength = Math.Clamp(value, 1, MAX_LINES);
            if (poolIndex >= trailLength) poolIndex = 0;
            settings.Length = trailLength;
            settings.Save();
        }

        private static System.Drawing.Color ToDrawingColor(System.Windows.Media.Color c) =>
            System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);

        // The dialog runs on its own STA thread so the overlay keeps rendering while it is open
        private void PickColor()
        {
            if (colorPickerOpen) return;
            colorPickerOpen = true;

            var initial = ToDrawingColor(trailColor.Color);
            var thread = new System.Threading.Thread(() =>
            {
                System.Drawing.Color? picked = null;
                try
                {
                    // Invisible topmost owner keeps the dialog above the topmost overlay window
                    using var owner = new System.Windows.Forms.Form
                    {
                        TopMost = true,
                        ShowInTaskbar = false,
                        FormBorderStyle = System.Windows.Forms.FormBorderStyle.None,
                        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                        Size = new System.Drawing.Size(1, 1),
                        Opacity = 0
                    };
                    owner.Show();
                    using var dialog = new System.Windows.Forms.ColorDialog { Color = initial, FullOpen = true };
                    if (dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK)
                        picked = dialog.Color;
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        colorPickerOpen = false;
                        if (picked.HasValue) ApplyColor(picked.Value);
                    }));
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        private void ApplyColor(System.Drawing.Color color)
        {
            var wpfColor = System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
            trailColor = new SolidColorBrush(wpfColor);
            trailColor.Freeze();
            settings.Color = wpfColor.ToString();
            settings.Save();

            var oldIcon = trayIcon.Icon;
            trayIcon.Icon = TrayIconFactory.Create(color);
            oldIcon?.Dispose();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                extendedStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED
                | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);

            CoverVirtualScreen();
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(CoverVirtualScreen));
        }

        // A DPI change (e.g. window moved between monitors) makes WPF resize the window; re-apply our bounds.
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            CoverVirtualScreen();
        }

        // Stretches the single overlay window across every monitor (positions are in physical pixels).
        private void CoverVirtualScreen()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            virtualLeft = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
            virtualTop = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
            int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, virtualLeft, virtualTop, width, height,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            dpiScaleX = dpi.DpiScaleX;
            dpiScaleY = dpi.DpiScaleY;
        }

        private void OnRender(object? sender, EventArgs e)
        {
            double now = frameClock.Elapsed.TotalSeconds;
            double dt = now - lastFrameSeconds;
            lastFrameSeconds = now;

            if (NativeMethods.GetCursorPos(out NativeMethods.POINT mousePos))
            {
                // Physical screen pixels -> window DIPs (PointFromScreen is unreliable across mixed-DPI monitors)
                var wpfPos = new System.Windows.Point(
                    (mousePos.X - virtualLeft) / dpiScaleX,
                    (mousePos.Y - virtualTop) / dpiScaleY);

                if (!lastMousePos.HasValue) { lastMousePos = wpfPos; return; }

                double deltaX = wpfPos.X - lastMousePos.Value.X;
                double deltaY = wpfPos.Y - lastMousePos.Value.Y;

                if ((deltaX * deltaX) + (deltaY * deltaY) > 100)
                {
                    // Reuse the next available line from the pool instead of creating a new object
                    Line segment = linePool[poolIndex];

                    segment.X1 = lastMousePos.Value.X;
                    segment.Y1 = lastMousePos.Value.Y;
                    segment.X2 = wpfPos.X;
                    segment.Y2 = wpfPos.Y;

                    segment.Stroke = trailColor;
                    segment.StrokeThickness = trailThickness;
                    segment.Opacity = START_OPACITY;
                    anyVisible = true;

                    poolIndex++;
                    if (poolIndex >= trailLength) poolIndex = 0;

                    lastMousePos = wpfPos;
                }
            }

            if (!anyVisible) return;

            // Time-based fade: independent of refresh rate; a longer trail lives longer
            double fade = dt * START_OPACITY / (trailLength * FADE_SECONDS_PER_LENGTH);
            bool stillVisible = false;
            for (int i = 0; i < MAX_LINES; i++)
            {
                double opacity = linePool[i].Opacity;
                if (opacity > 0)
                {
                    opacity = Math.Max(0, opacity - fade);
                    linePool[i].Opacity = opacity;
                    if (opacity > 0) stillVisible = true;
                }
            }
            anyVisible = stillVisible;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Icon?.Dispose();
                trayIcon.Dispose();
            }
            CompositionTarget.Rendering -= OnRender;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            base.OnClosed(e);
        }
    }
}