﻿using System;
using System.Numerics;
using NitroSharp.NsScript.Primitives;

namespace NitroSharp.NsScript.VM
{
    public abstract class BuiltInFunctions
    {
        internal NsScriptVM? _vm;
        private readonly Random _randomGen;

        protected BuiltInFunctions()
        {
            _randomGen = new Random();
        }

        public NsScriptProcess CurrentProcess => VM.CurrentProcess!;
        public NsScriptThread CurrentThread => CurrentProcess.CurrentThread!;
        protected NsScriptVM VM => _vm!;

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
        public virtual int GetHeight(in EntityPath entityPath) => throw new NotImplementedException();
        public virtual int GetWidth(in EntityPath entityPath) => throw new NotImplementedException();
        public virtual int GetMediaDuration(in EntityPath entityPath) => 0;
        public virtual int GetTimeRemaining(EntityQuery query) => throw new NotImplementedException();
        public virtual int GetTimeElapsed(in EntityPath entityPath) => throw new NotImplementedException();

        public virtual ConstantValue FormatString(string format, object[] args) => throw new NotImplementedException();

        public virtual void CreateEntity(in EntityPath path) { }
        public virtual void CreateThread(in EntityPath entityPath, string target) { }
        public virtual void SetAlias(in EntityPath entityPath, in EntityPath alias) { }
        public virtual void Request(EntityQuery query, NsEntityAction action) { }
        public virtual void DestroyEntities(EntityQuery query) { }

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
        public virtual void WaitText(EntityQuery query, TimeSpan timeout) { }

        public virtual void BoxBlur(EntityQuery query, uint nbPasses) { }
        public virtual void Grayscale(EntityQuery query) { }

        public virtual void CreateEffect(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, uint width, uint height, string effectName) { }

        public virtual void CreateCube(in EntityPath entityPath, int priority, string front, string back, string right, string left, string top, string bottom) { }
        public virtual void SetFieldOfView(string unk1, double unk2) { }
        public virtual void MoveCube(EntityQuery query, TimeSpan duration, NsNumeric dstX, NsNumeric dstY, NsNumeric dstZ, NsEaseFunction easeFunction, TimeSpan delay) { }

        public virtual void PlayVideo(in EntityPath entityPath, int priority, NsCoordinate x, NsCoordinate y, bool loop, bool alpha, string source) { }
        public virtual void WaitPlay(in EntityPath entityPath) { }

        /// <summary>
        /// Original name: CreateSound.
        /// </summary>
        public virtual void LoadAudio(in EntityPath entityPath, NsAudioKind kind, string fileName) { }

        /// <summary>
        /// Original name: SetLoop.
        /// </summary>
        public virtual void ToggleLooping(EntityQuery query, bool enable) { }

        public virtual void SetLoopRegion(in EntityPath entityPath, TimeSpan loopStart, TimeSpan loopEnd) { }
        public virtual void SetVolume(EntityQuery query, TimeSpan duration, NsRational volume) { }

        public virtual void Fade(EntityQuery query, TimeSpan duration, NsRational dstOpacity, NsEaseFunction easeFunction, TimeSpan delay) { }
        public virtual void Move(EntityQuery query, TimeSpan duration, NsCoordinate dstX, NsCoordinate dstY, NsEaseFunction easeFunction, TimeSpan delay) { }
        public virtual void Zoom(EntityQuery query, TimeSpan duration, NsRational dstScaleX, NsRational dstScaleY, NsEaseFunction easeFunction, TimeSpan delay) { }
        public virtual void Rotate(EntityQuery query, TimeSpan duration, NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ, NsEaseFunction easeFunction, TimeSpan delay) { }
        public virtual void BezierMove(EntityQuery query, TimeSpan duration, CompositeBezier curve, NsEaseFunction easeFunction, bool wait) { }
        public virtual void BeginTransition(EntityQuery query, TimeSpan duration, NsRational srcFadeAmount, NsRational dstFadeAmount, NsRational feather, NsEaseFunction easeFunction, string maskFileName, TimeSpan delay) { }
        public virtual void Shake(EntityQuery query, TimeSpan duration, NsCoordinate startX, NsCoordinate startY, NsCoordinate endX, NsCoordinate endY, uint freq, NsEaseFunction easeFunction, TimeSpan delay) { }

        public virtual void WaitAction(EntityQuery query, TimeSpan? timeout) { }
        public virtual void WaitMove(EntityQuery query) { }

        public virtual void CreateChoice(in EntityPath entityPath) { }
        public virtual void SetNextFocus(in EntityPath first, in EntityPath second, NsFocusDirection focusDirection) { }
        public virtual void SelectEnd() { }

        public virtual void CreateScrollbar(in EntityPath path, int priority, int x1, int y1, int x2, int y2, NsRational initialValue, NsScrollDirection scrollDirection, string knobImage) { }
        public virtual void SetScrollbar(in EntityPath scrollbar, in EntityPath parent) { }
        public virtual float GetScrollbarValue(in EntityPath scrollbarEntity) => 0;

        public virtual void Exit() { }

        public virtual void MoveCursor(int x, int y) { }
        public virtual Vector2 GetCursorPosition() => Vector2.Zero;

        public virtual void CreateBacklog(in EntityPath path, int priority) { }
        public virtual void SetBacklog(string text) { }
        public virtual void ClearBacklog() { }

        public virtual Vector2 GetPosition(in EntityPath entityPath) => Vector2.Zero;

        // --- NitroSharp only ---
        public virtual void ClearDialoguePage(in EntityPath dialoguePage) { }
        public virtual void AppendDialogue(in EntityPath dialoguePage, string markup) { }
        public virtual bool HandleInputEvents(in EntityPath uiElementPath) => false;
        public virtual void AssertTrue(bool value) { }

        public virtual void LineEnd(in EntityPath dialoguePage) { }

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
    }
}
