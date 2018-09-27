using System.Diagnostics;
using NitroSharp.Media.Decoding;
using Veldrid;

namespace NitroSharp.Media
{
    internal struct PlaybackState
    {
        public Stopwatch Stopwatch;
        public MediaPlaybackSession PlaybackSession;
        public Clock ExternalClock;
        public double SeekTarget;
        public bool Seeking;
        public bool HasAudio;

        public static void Initialize(ref PlaybackState state, MediaPlaybackSession playbackSession)
        {
            state.PlaybackSession = playbackSession;
            state.Stopwatch = new Stopwatch();
            state.ExternalClock = new Clock(state.Stopwatch);
            state.HasAudio = playbackSession.Container.HasAudio;
        }
    }

    internal struct AudioState
    {
        public AudioStream AudioStream;
        public AudioSource AudioSource;
        public MediaFrameQueue<MediaFrame> SampleQueue;
    }

    internal struct VideoState
    {
        public VideoStream VideoStream;
        public MediaFrameQueue<MediaFrame> FrameQueue;
        public Texture VideoTexture;
        public Texture StagingTexture;
        public TextureView TextureView;
        public Clock VideoClock;
        public double NextRefreshTime;
    }
}
