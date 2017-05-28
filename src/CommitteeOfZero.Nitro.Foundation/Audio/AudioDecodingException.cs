using System;

namespace CommitteeOfZero.Nitro.Foundation.Audio
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
