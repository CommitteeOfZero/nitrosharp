using MoeGame.Framework;

namespace CommitteeOfZero.Nitro
{
    public class NitroConfiguration : GameParameters
    {
        public string ContentRoot { get; set; }
        public string StartupScript { get; set; }
        public bool VSyncOn { get; set; }
    }
}
