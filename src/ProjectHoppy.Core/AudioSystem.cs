using ProjectHoppy.Core.Content;
using SciAdvNet.MediaLayer.Audio;
using System.Collections.Generic;

namespace ProjectHoppy.Core
{
    public class AudioSystem : EntityProcessingSystem
    {
        private readonly AudioEngine _audioEngine;
        private readonly ContentManager _content;

        private Dictionary<SoundComponent, AudioSource> _audioSrc;

        public AudioSystem(AudioEngine audioEngine, ContentManager content)
            : base(typeof(SoundComponent))
        {
            _audioEngine = audioEngine;
            _content = content;
            EntityAdded += OnEntityAdded;

            _audioSrc = new Dictionary<SoundComponent, AudioSource>();
        }

        private void OnEntityAdded(object sender, Entity e)
        {
            var sound = e.GetComponent<SoundComponent>();
            var assetComponent = e.GetComponent<AssetComponent>();
            if (assetComponent != null)
            {
                //if (assetComponent.FilePath.Contains("03"))
                var stream = _content.Load<AudioStream>(assetComponent.FilePath + ".ogg");
                var audioSource = _audioEngine.ResourceFactory.CreateAudioSource();
                audioSource.SetStream(stream);

                _audioSrc[sound] = audioSource;
            }
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var sound = entity.GetComponent<SoundComponent>();
            var src = _audioSrc[sound];
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

            //var asset = entity.GetComponent<AssetComponent>();
            //if (asset != null && !sound.Loaded)
            //{
            //    if (_content.IsLoaded(asset.FilePath))
            //    {
            //        sound.Loaded = true;

            //        var stream = _content.Get<AudioStream>(asset.FilePath);
            //        var audioSource = _audioEngine.ResourceFactory.CreateAudioSource();
            //        audioSource.SetStream(stream);
            //        //audioSource.Play();
            //    }
            //}
        }
    }
}
