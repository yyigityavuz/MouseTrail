using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MouseTrail
{
    // Draws the tray icon in code (a fading trail ending in a dot) so it can follow the chosen trail color
    public static class TrayIconFactory
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Icon Create(System.Drawing.Color color, int size = 32)
        {
            using var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                // Trail segments get thicker and more opaque towards the "cursor" end
                const int segments = 5;
                for (int i = 0; i < segments; i++)
                {
                    float t0 = i / (float)segments, t1 = (i + 1) / (float)segments;
                    var p0 = new PointF(size * (0.12f + 0.62f * t0), size * (0.85f - 0.55f * t0 * t0 - 0.2f * t0));
                    var p1 = new PointF(size * (0.12f + 0.62f * t1), size * (0.85f - 0.55f * t1 * t1 - 0.2f * t1));
                    int alpha = 60 + (int)(195 * t1);
                    using var pen = new Pen(System.Drawing.Color.FromArgb(alpha, color), size * (0.10f + 0.14f * t1))
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };
                    g.DrawLine(pen, p0, p1);
                }

                float r = size * 0.17f;
                using var head = new SolidBrush(color);
                g.FillEllipse(head, size * 0.74f - r, size * 0.30f - r, r * 2, r * 2);
            }

            // Icon.FromHandle does not own the HICON, so clone it and free the original
            IntPtr hIcon = bitmap.GetHicon();
            try
            {
                using var temp = Icon.FromHandle(hIcon);
                return (Icon)temp.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
    }
}
