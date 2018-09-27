using System;

namespace NitroSharp.Media
{
    internal struct MediaClipLoopData
    {
        public (TimeSpan loopStart, TimeSpan loopEnd)? LoopRegion;
        public bool LoopingEnabled;

        public MediaClipLoopData(bool loopingEnabled, (TimeSpan loopStart, TimeSpan loopEnd)? loopRegion)
        {
            LoopRegion = loopRegion;
            LoopingEnabled = loopingEnabled;
        }
    }
}
