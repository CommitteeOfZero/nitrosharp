using System;

namespace NitroSharp.Media.Decoding
{
    public class FFmpegException : Exception
    {
        public FFmpegException(string message) : base(message)
        {
        }

        public FFmpegException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
