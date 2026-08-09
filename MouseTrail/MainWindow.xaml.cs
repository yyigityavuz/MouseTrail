using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

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
        private SolidColorBrush trailColor = System.Windows.Media.Brushes.MediumPurple;
        private double trailThickness = 12;
        private int trailLength = 40;

        private System.Windows.Forms.NotifyIcon trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
            CompositionTarget.Rendering += OnRender;

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
            trayIcon.Icon = System.Drawing.SystemIcons.Application;
            trayIcon.Visible = true;
            trayIcon.Text = "Mouse Trail";

            var menu = new System.Windows.Forms.ContextMenuStrip();

            menu.Items.Add("Select Color", null, (s, e) => PickColor());

            var thicknessMenu = new System.Windows.Forms.ToolStripMenuItem("Set Thickness");
            thicknessMenu.DropDownItems.Add("Very Thin (6)", null, (s, e) => trailThickness = 6);
            thicknessMenu.DropDownItems.Add("Normal (12)", null, (s, e) => trailThickness = 12);
            thicknessMenu.DropDownItems.Add("Thick (24)", null, (s, e) => trailThickness = 24);
            thicknessMenu.DropDownItems.Add("Very Thick (40)", null, (s, e) => trailThickness = 40);
            menu.Items.Add(thicknessMenu);

            var lengthMenu = new System.Windows.Forms.ToolStripMenuItem("Set Length");
            lengthMenu.DropDownItems.Add("Short (20)", null, (s, e) => trailLength = 20);
            lengthMenu.DropDownItems.Add("Normal (40)", null, (s, e) => trailLength = 40);
            lengthMenu.DropDownItems.Add("Long (80)", null, (s, e) => trailLength = 80);
            menu.Items.Add(lengthMenu);

            menu.Items.Add("-");
            menu.Items.Add("Exit", null, (s, e) => System.Windows.Application.Current.Shutdown());

            trayIcon.ContextMenuStrip = menu;
        }

        private void PickColor()
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var wpfColor = System.Windows.Media.Color.FromArgb(
                    colorDialog.Color.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);

                trailColor = new SolidColorBrush(wpfColor);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                extendedStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
        }

        private void OnRender(object sender, EventArgs e)
        {
            if (NativeMethods.GetCursorPos(out NativeMethods.POINT mousePos))
            {
                System.Windows.Point wpfPos = PointFromScreen(new System.Windows.Point(mousePos.X, mousePos.Y));

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
                    segment.Opacity = 0.9;

                    poolIndex++;
                    if (poolIndex >= trailLength) poolIndex = 0;

                    lastMousePos = wpfPos;
                }
            }

            for (int i = 0; i < MAX_LINES; i++)
            {
                if (linePool[i].Opacity > 0)
                {
                    linePool[i].Opacity -= 0.04;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            CompositionTarget.Rendering -= OnRender;
            base.OnClosed(e);
        }
    }
}