using System;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public class ContentLoadException : Exception
    {
        public ContentLoadException()
        {
        }

        public ContentLoadException(string message)
            : base(message)
        {
        }

        public ContentLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
