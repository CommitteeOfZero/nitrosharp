using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using NitroSharp.NsScript.Primitives;

namespace NitroSharp.NsScript.VM
{
    public abstract class BuiltInFunctions
    {
        internal NsScriptVM? _vm;
        private readonly Random _randomGen = new();

        protected NsScriptVM VM => _vm!;

        [NotNull] public NsxModule? CurrentModule { get; internal set; }

        /// <summary>
        /// Original name: Random.
        /// </summary>
        public virtual int GetRandomNumber(int max) => _randomGen.Next(max);

        /// <summary>
        /// Original name: Platform.
        /// </summary>
        public virtual int GetPlatformId() => 100;

        public virtual string GetCurrentModuleName() => throw new NotImplementedException();
        public virtual int GetSoundAmplitude(string characterName) => throw new NotImplementedException();
        public virtual int GetHeight(in EntityQuery query) => throw new NotImplementedException();
        public virtual int GetWidth(in EntityQuery query) => throw new NotImplementedException();
        public virtual int GetMediaDuration(in EntityQuery query) => 0;
        public virtual int GetTimeRemaining(in EntityQuery query) => throw new NotImplementedException();
        public virtual int GetTimeElapsed(in EntityQuery query) => throw new NotImplementedException();

        public virtual ConstantValue FormatString(string format, object[] args) => throw new NotImplementedException();

        public virtual void CreateEntity(in EntityPath entityPath) { }
        public virtual void CreateThread(in EntityPath entityPath, string target, NsCoordinate x, NsCoordinate y) { }
        public virtual void SetAlias(in EntityQuery query, in EntityAlias alias) { }
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

        /// <summary>
        /// Original name: WaitKey.
        /// </summary>
        public virtual void WaitForInput(TimeSpan timeout) { }

        public virtual void LoadImage(in EntityPath entityPath, string source) { }
        public virtual void LoadColor(in EntityPath entityPath, uint width, uint height, NsColor color) { }

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        public virtual void CreateRectangle(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, uint width, uint height, NsColor color) { }

        /// <summary>
        /// Original name: CreateTexture.
        /// </summary>
        public virtual void CreateSprite(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, string source) { }

        public virtual void CreateSpriteEx(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, uint srcX, uint srcY, uint width, uint height, string source) { }

        public virtual void CreateAlphaMask(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, string imagePath, bool inheritTransform) { }

        /// <summary>
        /// Original name: CreateWindow.
        /// </summary>
        public virtual void CreateDialogueBox(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, uint width, uint height, bool inheritTransform) { }

        /// <summary>
        /// Original name: CreateText.
        /// </summary>
        public virtual void CreateTextBlock(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, NsTextDimension width, NsTextDimension height, string markup) { }

        public virtual void SetFont(string family, uint size, NsColor color, NsColor outlineColor, NsFontWeight weight, NsOutlineOffset outlineOffset) { }
        public virtual void LoadDialogueBlock(in DialogueBlockToken blockToken, uint maxWidth, uint maxHeight, int letterSpacing, int lineSpacing) { }
        public virtual void WaitText(in EntityQuery query, TimeSpan timeout) { }

        public virtual void BoxBlur(in EntityQuery query, uint nbPasses) { }
        public virtual void Grayscale(in EntityQuery query) { }

        public virtual void CreateEffect(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, uint width, uint height, string effectName) { }

        public virtual void CreateCube(in EntityPath entityPath, int priority, string front, string back, string right, string left, string top, string bottom) { }
        public virtual void SetFieldOfView(string unk1, double unk2) { }
        public virtual void MoveCube(in EntityQuery query, TimeSpan duration, NsNumeric dstX, NsNumeric dstY, NsNumeric dstZ, NsEaseFunction easeFunction, TimeSpan delay) { }

        public virtual void PlayVideo(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, bool loop, bool alpha, string source) { }
        public virtual void WaitPlay(in EntityQuery query) { }

        /// <summary>
        /// Original name: CreateSound.
        /// </summary>
        public virtual void LoadAudio(in EntityPath entityPath, NsAudioKind kind, string fileName) { }

        /// <summary>
        /// Original name: SetLoop.
        /// </summary>
        public virtual void ToggleLooping(in EntityQuery query, bool enable) { }

        public virtual void SetLoopRegion(in EntityQuery query, TimeSpan loopStart, TimeSpan loopEnd) { }
        public virtual void SetVolume(in EntityQuery query, TimeSpan duration, NsRational volume) { }

        public virtual void Fade(in EntityQuery query, TimeSpan duration, NsRational dstOpacity, NsEaseFunction easeFunction, TimeSpan delay) { }
        public virtual void Move(in EntityQuery query, TimeSpan duration, NsCoordinate dstX, NsCoordinate dstY, NsEaseFunction easeFunction, TimeSpan delay) { }
        public virtual void Zoom(in EntityQuery query, TimeSpan duration, NsRational dstScaleX, NsRational dstScaleY, NsEaseFunction easeFunction, TimeSpan delay) { }
        public virtual void Rotate(in EntityQuery query, TimeSpan duration, NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ, NsEaseFunction easeFunction, TimeSpan delay) { }
        public virtual void BezierMove(in EntityQuery query, TimeSpan duration, CompositeBezier curve, NsEaseFunction easeFunction, bool wait) { }
        public virtual void BeginTransition(in EntityQuery query, TimeSpan duration, NsRational srcFadeAmount, NsRational dstFadeAmount, NsRational feather, NsEaseFunction easeFunction, string maskFileName, TimeSpan delay) { }
        public virtual void Shake(in EntityQuery query, TimeSpan duration, NsCoordinate startX, NsCoordinate startY, NsCoordinate endX, NsCoordinate endY, uint freq, NsEaseFunction easeFunction, TimeSpan delay) { }

        public virtual void WaitAction(in EntityQuery query, TimeSpan? timeout) { }
        public virtual void WaitMove(in EntityQuery query) { }

        public virtual void CreateChoice(in EntityPath entityPath) { }
        public virtual void SetNextFocus(in EntityQuery first, in EntityQuery second, NsFocusDirection focusDirection) { }
        public virtual void SelectEnd() { }

        public virtual void CreateScrollbar(in EntityPath path, int priority, int x1, int y1, int x2, int y2, NsRational initialValue, NsScrollDirection scrollDirection, string knobImage) { }
        public virtual void SetScrollbar(in EntityQuery scrollbar, in EntityQuery parent) { }
        public virtual float GetScrollbarValue(in EntityQuery scrollbarEntity) => 0;

        public virtual void Exit() { }

        public virtual void MoveCursor(int x, int y) { }
        public virtual Vector2 GetCursorPosition() => Vector2.Zero;

        public virtual void CreateBacklog(in EntityPath path, int priority) { }
        public virtual void SetBacklog(string text) { }
        public virtual void ClearBacklog() { }

        public virtual Vector2 GetPosition(in EntityQuery query) => Vector2.Zero;

        // --- NitroSharp only ---
        public virtual bool HandleInputEvents(in EntityQuery uiElementPath) => false;
        public virtual void AssertTrue(bool value) { }

        public virtual bool SaveExists(uint slot) => false;
        public virtual bool MountSaveData(uint slot) => false;
        public virtual void SaveGame(uint slot) { }
        public virtual void LoadGame(uint slot) { }
        public virtual void DeleteSave(uint slot) { }

        public virtual int GetSecondsElapsed() => 0;

        public virtual bool FileExists(string path) => false;

        public virtual DateTime GetDateTime() => DateTime.Now;

        public virtual bool X360_IsSignedIn() => true;
        public virtual bool X360_UserDataExists() => false;
        public virtual bool X360_CheckStorage() => true;

        public virtual float X360_GetTriggerAxis(XboxTrigger trigger) => 0;
        public virtual void Reset() { }

        public virtual void Draw() { }
    }
}
