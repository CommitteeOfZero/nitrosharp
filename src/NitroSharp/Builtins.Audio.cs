using NitroSharp.NsScript;

namespace NitroSharp
{
    internal partial class Builtins
    {
        public override int GetSoundAmplitude(string characterName) => 0;
        public override int GetSoundDuration(in EntityPath entityPath) => 0;
        public override int GetTimeElapsed(in EntityPath entityPath) => 0;
        public override int GetTimeRemaining(in EntityPath entityPath) => 0;
    }
}
