using System;
using NitroSharp.NsScript.Primitives;

namespace NitroSharp.NsScript.VM
{
    public abstract class BuiltInFunctions
    {
        protected BuiltInFunctions()
        {
            _randomGen = new Random();
        }

        public virtual int GenerateRandomNumber(int max) => _randomGen.Next(max);

        internal NsScriptVM? _vm;
        private readonly Random _randomGen;

        public NsScriptVM VM => _vm!;

        public ThreadContext? MainThread => VM.MainThread;
        public ThreadContext? CurrentThread => VM.CurrentThread;

        public virtual void BeginDialogueLine(string pxmlString) { }

        public virtual int GetPlatformId() => 0;
        public virtual string GetCurrentModuleName() => throw new NotImplementedException();
        //public virtual int GenerateRandomNumber(int max) => throw new NotImplementedException();
        public virtual int GetSoundAmplitude(string characterName) => throw new NotImplementedException();
        public virtual int GetHeight(in EntityPath entityPath) => throw new NotImplementedException();
        public virtual int GetWidth(in EntityPath entityPath) => throw new NotImplementedException();
        public virtual int GetSoundDuration(in EntityPath entityPath) => 0;
        public virtual int GetTimeRemaining(in EntityPath entityPath) => throw new NotImplementedException();
        public virtual int GetTimeElapsed(in EntityPath entityPath) => throw new NotImplementedException();

        public virtual void SetAlias(in EntityPath entityPath, in EntityPath alias) { }
        public virtual void Request(in EntityQuery query, NsEntityAction action) { }
        public virtual void DestroyEntities(in EntityQuery query) { }

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
        public virtual void CreateDialogueBox(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, int width, int height) { }

        /// <summary>
        /// Original name: CreateTexture.
        /// </summary>
        public virtual void CreateSprite(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, string fileOrExistingEntityName) { }

        public virtual void CreateSpriteEx(in EntityPath entityPath, int priority, NsCoordinate x1, NsCoordinate y1, NsCoordinate x2, NsCoordinate y2, int width, int height, in EntityPath srcEntityPath) { }

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        public virtual void CreateRectangle(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, int width, int height, NsColor color) { }

        /// <summary>
        /// Original name: CreateText.
        /// </summary>
        public virtual void CreateTextBlock(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, NsDimension width, NsDimension height, string pxmlText) { }

        public virtual void CreateEffect(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, int width, int height, string effectName) { }

        public virtual void CreateAlphaMask(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, string path, bool unk) { }

        public virtual void BoxBlur(in EntityQuery query, uint nbPasses) { }
        public virtual void Grayscale(in EntityQuery query) { }

        public virtual void CreateCube(in EntityPath entityPath, int priority, string front, string back, string right, string left, string top, string bottom) { }
        public virtual void SetFieldOfView(string unk1, double unk2) { }

        public virtual void WaitPlay(in EntityPath entityPath) { }

        public virtual void LoadVideo(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, bool loop, string fileName) { }

        /// <summary>
        /// Original name: CreateSound.
        /// </summary>
        public virtual void LoadAudio(in EntityPath entityPath, NsAudioKind kind, string fileName) { }
        public virtual void LoadImage(in EntityPath entityPath, string fileName) { }

        /// <summary>
        /// Original name: SetLoop.
        /// </summary>
        public virtual void ToggleLooping(in EntityPath entityPath, bool looping)
        {
        }

        public virtual void SetLoopRegion(in EntityPath entityPath, TimeSpan loopStart, TimeSpan loopEnd) { }
        public virtual void SetVolume(in EntityPath entityPath, TimeSpan duration, NsRational volume) { }

        public virtual void Fade(in EntityQuery query, TimeSpan duration, NsRational dstOpacity, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void Move(in EntityQuery query, TimeSpan duration, NsCoordinate dstX, NsCoordinate dstY, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void Zoom(in EntityQuery query, TimeSpan duration, NsRational dstScaleX, NsRational dstScaleY, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void Rotate(in EntityQuery query, TimeSpan duration, NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void MoveCube(in EntityQuery query, TimeSpan duration, NsNumeric dstTranslationX, NsNumeric dstTranslationY, NsNumeric dstTranslationZ, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void BezierMove(in EntityQuery query, TimeSpan duration, CompositeBezier curve, NsEasingFunction easingFunction, bool wait) { }
        public virtual void DrawTransition(in EntityQuery query, TimeSpan duration, NsRational initialFadeAmount, NsRational finalFadeAmount, NsRational feather, NsEasingFunction easingFunction, string maskFileName, TimeSpan delay) { }

        public virtual void CreateThread(in EntityPath entityPath, string target) { }

        public virtual ConstantValue FormatString(string format, object[] args) => throw new NotImplementedException();

        public virtual float GetScrollbarValue(in EntityPath scrollbarEntity)
        {
            return 0.5f;
        }

        public virtual void CreateChoice(in EntityPath entityPath) { }
        public virtual bool IsPressed(string choice) => false;

        public virtual void PlayCutscene(in EntityPath entityPath, int priority, bool loop, bool alpha, string fileName, bool enableAudio) { }

        public virtual void LoadText(in DialogueBlockToken token, int maxWidth, int maxHeight, int letterSpacing, int lineSpacing) { }

        public virtual void AssertTrue(bool value) { }

        public virtual void CreateEntity(in EntityPath path) {}
    }
}
