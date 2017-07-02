namespace NitroSharp.Foundation.Audio
{
    public abstract class ResourceFactory
    {
        public abstract AudioSource CreateAudioSource(uint bufferSize);
    }
}
