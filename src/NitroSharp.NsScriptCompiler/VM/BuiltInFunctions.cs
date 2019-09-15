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

        internal VirtualMachine? _vm;
        private readonly Random _randomGen;

        public VirtualMachine Interpreter => _vm!;

        public ThreadContext? MainThread => Interpreter.MainThread;
        public ThreadContext? CurrentThread => Interpreter.CurrentThread;

        public virtual void BeginDialogueLine(string pxmlString) { }

        public virtual int GetPlatformId() => 0;
        public virtual string GetCurrentModuleName() => throw new NotImplementedException();
        //public virtual int GenerateRandomNumber(int max) => throw new NotImplementedException();
        public virtual int GetSoundAmplitude(string characterName) => throw new NotImplementedException();
        public virtual int GetHeight(string entityName) => throw new NotImplementedException();
        public virtual int GetWidth(string entityName) => throw new NotImplementedException();
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
        public virtual void CreateSprite(string entityName, int priority, NsCoordinate x, NsCoordinate y, string fileOrExistingEntityName) { }

        public virtual void SetFieldOfView(string unk1, double unk2) { }

        public virtual void CreateCube(string entityName, int priority, string front, string back, string right, string left, string top, string bottom) { }

        public virtual void WaitPlay(string entityName) { }

        public virtual void LoadVideo(string entityName, int priority, NsCoordinate x, NsCoordinate y, bool loop, string fileName) { }

        public virtual void CreateText(string entityName, int priority, NsCoordinate x, NsCoordinate y, NsDimension width, NsDimension height, string pxmlText) { }

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        public virtual void FillRectangle(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, NsColor color) { }

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
        public virtual void Fade(string entityName, TimeSpan duration, NsRational dstOpacity, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void Move(string entityName, TimeSpan duration, NsCoordinate dstX, NsCoordinate dstY, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void CreateThread(string name, string target) { }
        public virtual void Zoom(string entityName, TimeSpan duration, NsRational dstScaleX, NsRational dstScaleY, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void DrawTransition(string sourceEntityName, TimeSpan duration, NsRational initialOpacity, NsRational finalOpacity, NsRational feather, NsEasingFunction easingFunction, string maskFileName, TimeSpan delay) { }

        public virtual ConstantValue FormatString(string format, object[] args) => throw new NotImplementedException();

        public virtual float GetScrollbarValue(string scrollbarEntity)
        {
            return 0.5f;
        }

        public virtual void CreateChoice(string entityName) { }
        public virtual void PlayCutscene(string entityName, int priority, bool loop, bool alpha, string fileName, bool enableAudio) { }
        public virtual void CreateSpriteEx(string entityName, int priority, NsCoordinate x1, NsCoordinate y1, NsCoordinate x2, NsCoordinate y2, int width, int height, string srcEntityName) { }

        public virtual void Rotate(string entityName, TimeSpan duration, NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ, NsEasingFunction easingFunction, TimeSpan delay) { }
        public virtual void MoveCube(string entityName, TimeSpan duration, NsNumeric dstTranslationX, NsNumeric dstTranslationY, NsNumeric dstTranslationZ, NsEasingFunction easingFunction, TimeSpan delay) { }

        public virtual void LoadText(in DialogueBlockToken token, int maxWidth, int maxHeight, int letterSpacing, int lineSpacing) { }

        public virtual void Select() { }
        public virtual string GetSelectedChoice() => throw new NotImplementedException();
    }
}
