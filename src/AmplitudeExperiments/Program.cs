using MoeGame.Framework.Audio;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AmplitudeExperiments
{
    class Program
    {
        static void Main(string[] args)
        {
            var fs = File.OpenRead("00700120ri.ogg");
            var audio = new FFmpegAudioStream(fs);

            audio.TargetBitDepth = 16;
            audio.TargetChannelCount = 1;
            audio.TargetSampleRate = 44100;
            audio.SetupResampler();

            //var raw = File.Create("voice.raw");
            //var writer = new BinaryWriter(raw);

            var oneSecondBuf = new AudioBuffer(0,44100);
            while (true)
            {
                bool succ = audio.Read(oneSecondBuf);
                if (!succ || oneSecondBuf.Position == 0)
                {
                    break;
                }

                int pos = 0;
                while (pos < oneSecondBuf.Position)
                {
                    int length = Math.Min(2200, oneSecondBuf.Position - pos);
                    //if (length < 2200)
                    //{
                    //    break;
                    //}
                    var chunk = new short[length];
                    Marshal.Copy(oneSecondBuf.StartPointer + pos, chunk, 0, length);

                    short first = chunk[0];
                    short second = chunk[length / 4];
                    short third = chunk[length / 4 + length / 2];
                    short fourth = chunk[length - 1];

                    double amp = Math.Abs(first) + Math.Abs(second) + Math.Abs(third) + Math.Abs(fourth);
                    amp /= 4;

                    //if (amp > 10)
                    {
                        Console.WriteLine(Math.Floor(amp));
                    }

                    pos += length;
                }


                oneSecondBuf.ResetPosition();
            }

            //writer.Dispose();
            //raw.Dispose();
        }
    }
}
