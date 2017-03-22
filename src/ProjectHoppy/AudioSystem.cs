using ProjectHoppy.Content;
using SciAdvNet.MediaLayer.Audio;
using System.Collections.Generic;

namespace ProjectHoppy
{
    public class AudioSystem : EntityProcessingSystem
    {
        private readonly AudioEngine _audioEngine;
        private readonly ContentManager _content;

        private Dictionary<SoundComponent, AudioSource> _audioSources;
        private Queue<AudioSource> _freeAudioSources;

        public AudioSystem(AudioEngine audioEngine, ContentManager content)
            : base(typeof(SoundComponent))
        {
            _audioEngine = audioEngine;
            _content = content;
            EntityAdded += OnEntityAdded;

            _audioSources = new Dictionary<SoundComponent, AudioSource>();
            _freeAudioSources = new Queue<AudioSource>();
        }

        private void OnEntityAdded(object sender, Entity e)
        {
            var sound = e.GetComponent<SoundComponent>();
            var assetComponent = e.GetComponent<AssetComponent>();
            if (assetComponent != null)
            {
                var stream = _content.Load<AudioStream>(assetComponent.FilePath + ".ogg");
                var audioSource = GetFreeAudioSource();
                audioSource.SetStream(stream);

                _audioSources[sound] = audioSource;
            }
        }

        private AudioSource GetFreeAudioSource()
        {
            return _freeAudioSources.Count > 0 ? _freeAudioSources.Dequeue() : _audioEngine.ResourceFactory.CreateAudioSource();
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var sound = entity.GetComponent<SoundComponent>();
            var src = _audioSources[sound];
            if (sound.Volume > 0 && !sound.Playing)
            {
                src.Play();
                sound.Playing = true;
            }

            if (sound.Looping && !src.CurrentStream.Looping)
            {
                if (sound.LoopEnd.TotalSeconds > 0)
                {
                    src.CurrentStream.SetLoop(sound.LoopStart, sound.LoopEnd);
                }
                else
                {
                    src.CurrentStream.SetLoop();
                }
            }
        }
    }
}
