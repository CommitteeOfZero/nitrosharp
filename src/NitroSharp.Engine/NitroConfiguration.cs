using NitroSharp.Foundation;

namespace NitroSharp
{
    public sealed class NitroConfiguration : GameParameters
    {
        public string ContentRoot { get; set; }
        public string StartupScript { get; set; }
        public bool VSyncOn { get; set; }
    }
}
