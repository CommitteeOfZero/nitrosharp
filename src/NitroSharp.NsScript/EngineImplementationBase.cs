using System;

namespace NitroSharp.NsScript
{
    public abstract class EngineImplementationBase
    {
        public virtual void DisplayDialogue(string pxmlString) { }

        public virtual int GetPlatformId() => throw new NotImplementedException();
        public virtual string GetCurrentModuleName() => throw new NotImplementedException();
        public virtual int GenerateRandomNumber(int max) => throw new NotImplementedException();
        public virtual int GetSoundAmplitude(string characterName) => throw new NotImplementedException();
        public virtual int GetTextureHeight(string entityName) => throw new NotImplementedException();
        public virtual int GetTextureWidth(string entityName) => throw new NotImplementedException();
        public virtual int GetSoundDuration(string entityName) => throw new NotImplementedException();
        public virtual int GetTimeRemaining(string soundEntityName) => throw new NotImplementedException();
        public virtual int GetTimeElapsed(string entityName) => throw new NotImplementedException();

        public virtual void SetAlias(string entityName, string alias) { }
        public virtual void Request(string entityName, NsEntityAction action) { }
        public virtual void RemoveEntity(string entityName) { }

        /// <summary>
        /// Original name: Wait.
        /// </summary>
        public virtual void Delay(TimeSpan delay) { }

        /// <summary>
        /// Original name: WaitKey.
        /// </summary>
        public virtual void WaitForInput() { }
        public virtual void WaitForInput(TimeSpan timeout) { }
        public virtual void WaitText(string id, TimeSpan time) { }

        /// <summary>
        /// Original name: CreateWindow.
        /// </summary>
        public virtual void CreateDialogueBox(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height) { }

        /// <summary>
        /// Original name: CreateText.
        /// </summary>
        public virtual void AddText(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, string text) { }

        /// <summary>
        /// Original name: CreateTexture.
        /// </summary>
        public virtual void AddTexture(string entityName, int priority, NsCoordinate x, NsCoordinate y, string fileOrExistingEntityName) { }

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        public virtual void AddRectangle(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, NsColor color) { }

        /// <summary>
        /// Original name: CreateSound.
        /// </summary>
        public virtual void LoadAudio(string entityName, NsAudioKind kind, string fileName) { }
        public virtual void LoadImage(string entityName, string fileName) { }

        /// <summary>
        /// Original name: SetLoop.
        /// </summary>
        public virtual void ToggleLooping(string entityName, bool looping)
        {
        }

        public virtual void SetLoopPoint(string entityName, TimeSpan loopStart, TimeSpan loopEnd) { }
        public virtual void SetVolume(string entityName, TimeSpan duration, NsRational volume) { }
        public virtual void Fade(string entityName, TimeSpan duration, NsRational opacity, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void Move(string entityName, TimeSpan duration, NsCoordinate x, NsCoordinate y, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void CreateThread(string name, string target) { }
        public virtual void Zoom(string entityName, TimeSpan duration, NsRational scaleX, NsRational scaleY, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void DrawTransition(string sourceEntityName, TimeSpan duration, NsRational initialOpacity, NsRational finalOpacity, NsRational feather, NsEasingFunction easingFunction, string maskFileName, TimeSpan delay) { }
        public virtual void CreateChoice(string entityName) { }
        public virtual void PlayCutscene(string entityName, int priority, bool loop, bool alpha, string fileName, bool enableAudio) { }
        public virtual void AddClippedTexture(string entityName, int priority, NsCoordinate x1, NsCoordinate y1, NsCoordinate x2, NsCoordinate y2, int width, int height, string srcEntityName) { }
    }
}
