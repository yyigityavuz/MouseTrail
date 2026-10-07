using Color = System.Windows.Media.Color;
using System.Windows.Media;

namespace MouseTrail
{
    // Derives the trail colors used while a mouse button is held, from the user's base color
    public static class ClickColors
    {
        // Left button: the inverse (complement) of the base color
        public static SolidColorBrush ForLeft(Color baseColor) =>
            Freeze(Color.FromArgb(baseColor.A, (byte)(255 - baseColor.R), (byte)(255 - baseColor.G), (byte)(255 - baseColor.B)));

        // Right button: base hue rotated by 120 degrees
        public static SolidColorBrush ForRight(Color baseColor) => Freeze(RotateHue(baseColor, 120));

        // Middle button: base hue rotated by 240 degrees
        public static SolidColorBrush ForMiddle(Color baseColor) => Freeze(RotateHue(baseColor, 240));

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static Color RotateHue(Color c, double degrees)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta > 0)
            {
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else h = 60 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;

            double s = max == 0 ? 0 : delta / max;
            double v = max;

            // Greys have no hue to rotate, so give them a visible one instead of returning the same color
            if (s < 0.15)
            {
                s = 0.8;
                v = Math.Max(v, 0.8);
            }

            h = (h + degrees) % 360;

            double chroma = v * s;
            double x = chroma * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - chroma;
            (double r1, double g1, double b1) = (int)(h / 60) switch
            {
                0 => (chroma, x, 0.0),
                1 => (x, chroma, 0.0),
                2 => (0.0, chroma, x),
                3 => (0.0, x, chroma),
                4 => (x, 0.0, chroma),
                _ => (chroma, 0.0, x)
            };

            return Color.FromArgb(c.A,
                (byte)Math.Round((r1 + m) * 255),
                (byte)Math.Round((g1 + m) * 255),
                (byte)Math.Round((b1 + m) * 255));
        }
    }
}
