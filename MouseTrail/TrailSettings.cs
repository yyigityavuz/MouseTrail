using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace MouseTrail
{
    // User settings persisted as JSON under %AppData%\MouseTrail
    public class TrailSettings
    {
        public string Color { get; set; } = "#9370DB"; // MediumPurple
        public double Thickness { get; set; } = 12;
        public int Length { get; set; } = 40;

        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MouseTrail", "settings.json");

        public static TrailSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<TrailSettings>(File.ReadAllText(FilePath)) ?? new TrailSettings();
            }
            catch { /* corrupt or unreadable file: fall back to defaults */ }
            return new TrailSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
            }
            catch { /* settings are best-effort */ }
        }

        public System.Windows.Media.Color ToColor()
        {
            try { return (System.Windows.Media.Color)ColorConverter.ConvertFromString(Color); }
            catch { return Colors.MediumPurple; }
        }
    }
}
