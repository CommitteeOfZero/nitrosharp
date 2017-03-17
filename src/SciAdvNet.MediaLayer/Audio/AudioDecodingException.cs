using System;

namespace SciAdvNet.MediaLayer.Audio
{
    public class AudioDecodingException : Exception
    {
        public AudioDecodingException()
        {
        }

        public AudioDecodingException(string message) : base(message)
        {
        }

        public AudioDecodingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
