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
        public GraphicsBackend? PreferredBackend { get; set; }

        public string ContentRoot { get; set; }
        public bool EnableDiagnostics { get; set; }
        public string StartupScript { get; set; }
    }
}
