namespace NitroSharp
{
    public sealed class NitroConfiguration : GameParameters
    {
        public string ProductName { get; set; } = "NitroSharp";
        public string ContentRoot { get; set; }
        public bool EnableDiagnostics { get; set; }
        public string StartupScript { get; set; }
        public bool VSyncOn { get; set; }
    }
}
