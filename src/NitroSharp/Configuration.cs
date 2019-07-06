using NitroSharp.Media;
using Veldrid;

namespace NitroSharp
{
    public sealed class Configuration
    {
        public string ProductName { get; set; } = "NitroSharp";
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public string WindowTitle { get; set; }

        public bool EnableVSync { get; set; }
        public GraphicsBackend? PreferredGraphicsBackend { get; set; }
        public AudioBackend? PreferredAudioBackend { get; set; }

        public string ContentRoot { get; set; }
        public bool EnableDiagnostics { get; set; }
        public string StartupScript { get; set; }
        public bool UseDedicatedInterpreterThread { get; set; } = true;
        public bool UseUtf8 { get; set; } = false;
        public bool SkipUpToDateCheck { get; set; } = false;
    }
}
