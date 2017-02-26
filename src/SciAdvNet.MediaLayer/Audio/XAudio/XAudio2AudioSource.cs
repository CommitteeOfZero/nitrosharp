using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SciAdvNet.MediaLayer.Audio.XAudio
{
    public class XAudio2AudioSource : AudioSource
    {
        private readonly XAudio2AudioEngine _engine;
        private SourceVoice _sourceVoice;

        internal XAudio2AudioSource(XAudio2AudioEngine engine)
        {
            _engine = engine;
            var waveFormat = new WaveFormat(44100, 32, 2);
            _sourceVoice = new SourceVoice(_engine.Device, waveFormat);
        }

        public override float Volume
        {
            get => _sourceVoice.Volume;
            set => _sourceVoice.SetVolume(value);
        }

        public override void Play(AudioFile file)
        {
            Task.Run(async () =>
            {
                var samples = new float[file.Channels * file.SampleRate];

                var bufferQueue = new Queue<AudioBuffer>();
                _sourceVoice.BufferEnd += (IntPtr _) =>
                {
                    bufferQueue.Dequeue().Stream.Dispose();
                };

                _sourceVoice.Start();

                bool doneReading = false;
                do
                {
                    if (_sourceVoice.State.BuffersQueued < 3 && !doneReading)
                    {
                        int bytesRead = file.ReadSamples(samples, 0, samples.Length);
                        if (bytesRead == 0)
                        {
                            doneReading = true;
                            continue;
                        }

                        var dataStream = new DataStream(bytesRead * sizeof(float), true, true);
                        dataStream.WriteRange(samples, 0, bytesRead);
                        dataStream.Position = 0;

                        var buffer = new AudioBuffer(dataStream);
                        buffer.Flags = BufferFlags.EndOfStream;
                        bufferQueue.Enqueue(buffer);
                        _sourceVoice.SubmitSourceBuffer(buffer, null);
                    }

                    await Task.Delay(100).ConfigureAwait(false);
                } while (_sourceVoice.State.BuffersQueued > 0);

                _sourceVoice.DestroyVoice();
                _sourceVoice.Dispose();
            });
        }
    }
}
