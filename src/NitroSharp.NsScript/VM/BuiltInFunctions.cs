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
        public virtual int GetHeight(string entityName) => throw new NotImplementedException();
        public virtual int GetWidth(string entityName) => throw new NotImplementedException();
        public virtual int GetSoundDuration(string entityName) => 0;
        public virtual int GetTimeRemaining(string soundEntityName) => throw new NotImplementedException();
        public virtual int GetTimeElapsed(string entityName) => throw new NotImplementedException();

        public virtual void SetAlias(string entityName, string alias) { }
        public virtual void Request(string entityQuery, NsEntityAction action) { }
        public virtual void DestroyEntities(string entityQuery) { }

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
        public virtual void CreateDialogueBox(string name, int priority, NsCoordinate x, NsCoordinate y, int width, int height) { }

        /// <summary>
        /// Original name: CreateTexture.
        /// </summary>
        public virtual void CreateSprite(string name, int priority, NsCoordinate x, NsCoordinate y, string fileOrExistingEntityName) { }

        public virtual void CreateSpriteEx(string name, int priority, NsCoordinate x1, NsCoordinate y1, NsCoordinate x2, NsCoordinate y2, int width, int height, string srcEntityName) { }

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        public virtual void CreateRectangle(string name, int priority, NsCoordinate x, NsCoordinate y, int width, int height, NsColor color) { }

        /// <summary>
        /// Original name: CreateText.
        /// </summary>
        public virtual void CreateTextBlock(string name, int priority, NsCoordinate x, NsCoordinate y, NsDimension width, NsDimension height, string pxmlText) { }

        public virtual void CreateEffect(string entity, int priority, NsCoordinate x, NsCoordinate y, int width, int height, string effectName) { }

        public virtual void CreateAlphaMask(string v, int priority, NsCoordinate x, NsCoordinate y, string path, bool unk) { }

        public virtual void BoxBlur(string entityQuery, uint nbPasses) { }
        public virtual void Grayscale(string entityQuery) { }

        public virtual void CreateCube(string name, int priority, string front, string back, string right, string left, string top, string bottom) { }
        public virtual void SetFieldOfView(string unk1, double unk2) { }

        public virtual void WaitPlay(string entityName) { }

        public virtual void LoadVideo(string entityName, int priority, NsCoordinate x, NsCoordinate y, bool loop, string fileName) { }

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

        public virtual void SetLoopRegion(string entityName, TimeSpan loopStart, TimeSpan loopEnd) { }
        public virtual void SetVolume(string entityName, TimeSpan duration, NsRational volume) { }

        public virtual void Fade(string entityQuery, TimeSpan duration, NsRational dstOpacity, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void Move(string entityQuery, TimeSpan duration, NsCoordinate dstX, NsCoordinate dstY, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void Zoom(string entityQuery, TimeSpan duration, NsRational dstScaleX, NsRational dstScaleY, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void Rotate(string entityQuery, TimeSpan duration, NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void MoveCube(string entityQuery, TimeSpan duration, NsNumeric dstTranslationX, NsNumeric dstTranslationY, NsNumeric dstTranslationZ, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void BezierMove(string entityQuery, TimeSpan duration, CompositeBezier curve, NsEasingFunction easingFunction, bool wait) { }
        public virtual void DrawTransition(string entityQuery, TimeSpan duration, NsRational initialFadeAmount, NsRational finalFadeAmount, NsRational feather, NsEasingFunction easingFunction, string maskFileName, TimeSpan delay) { }

        public virtual void CreateThread(string name, string target) { }

        public virtual ConstantValue FormatString(string format, object[] args) => throw new NotImplementedException();

        public virtual float GetScrollbarValue(string scrollbarEntity)
        {
            return 0.5f;
        }

        public virtual void CreateChoice(string name) { }
        public virtual bool IsPressed(string choice) => false;

        public virtual void PlayCutscene(string entityName, int priority, bool loop, bool alpha, string fileName, bool enableAudio) { }
        
        public virtual void LoadText(in DialogueBlockToken token, int maxWidth, int maxHeight, int letterSpacing, int lineSpacing) { }

        public virtual void AssertTrue(bool value) { }
    }
}
