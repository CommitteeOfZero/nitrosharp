using System;

namespace CommitteeOfZero.NitroSharp.Foundation.Audio
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
