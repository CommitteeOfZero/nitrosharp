using System;

namespace SciAdvNet.NSScript.Execution
{
    public abstract class NssImplementation
    {
        public ThreadContext CurrentThread { get; internal set; }
        public DialogueBlock CurrentDialogueBlock { get; internal set; }

        public event EventHandler<DialogueBlock> EnteredDialogueBlock;

        internal void RaiseEnteredDialogueBlock(DialogueBlock block)
        {
            EnteredDialogueBlock?.Invoke(this, block);
        }

        public virtual void DisplayDialogue(string pxmlString)
        {
        }

        public virtual void SetAlias(string entityName, string alias)
        {
        }

        public virtual void Request(string entityName, NssEntityAction action)
        {
        }

        public virtual void RemoveEntity(string entityName)
        {
        }

        /// <summary>
        /// Original name: Wait.
        /// </summary>
        /// <param name="delay"></param>
        public virtual void Delay(TimeSpan delay)
        {
        }

        /// <summary>
        /// Original name: WaitKey.
        /// </summary>
        public virtual void WaitForInput()
        {
        }

        public virtual void WaitForInput(TimeSpan timeout)
        {
        }

        public virtual void WaitText(string id, TimeSpan time)
        {
            CurrentThread.PushContinuation(CurrentDialogueBlock);
        }

        /// <summary>
        /// Original name: CreateWindow.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="priority"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public virtual void CreateDialogueBox(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height)
        {
        }

        /// <summary>
        /// Original name: CreateText.
        /// </summary>
        public virtual void AddText(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height, string text)
        {
        }

        /// <summary>
        /// Original name: CreateTexture.
        /// </summary>
        public virtual void AddTexture(string entityName, int priority, NssCoordinate x, NssCoordinate y, string fileOrEntityName)
        {
        }

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        public virtual void AddRectangle(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color)
        {
        }

        /// <summary>
        /// Original name: CreateSound.
        /// </summary>
        public virtual void LoadAudio(string entityName, AudioKind kind, string fileName)
        {
        }

        public virtual void LoadImage(string entityName, string fileName)
        {
        }

        /// <summary>
        /// Original name: SetLoop.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="looping"></param>
        public virtual void ToggleLooping(string entityName, bool loop)
        {
        }

        public virtual void SetLoopPoint(string entityName, TimeSpan loopStart, TimeSpan loopEnd)
        {
        }

        public virtual void SetVolume(string entityName, TimeSpan duration, int volume)
        {
        }

        public virtual void Fade(string entityName, TimeSpan duration, Rational opacity, bool wait)
        {
        }

        public virtual void Move(string entityName, TimeSpan duration, NssCoordinate x, NssCoordinate y, EasingFunction easingFunction, bool wait)
        {
        }

        public virtual void Zoom(string entityName, TimeSpan duration, Rational scaleX, Rational scaleY, EasingFunction easingFunction, bool wait)
        {
        }

        public virtual void DrawTransition(string entityName, TimeSpan duration, Rational initialOpacity, Rational finalOpacity, Rational feather, string fileName, bool wait)
        {
        }

        public virtual void CreateChoice(string entityName)
        {
        }

        public virtual void PlayCutscene(string entityName, int priority, bool loop, bool alpha, string fileName, bool enableAudio)
        {
        }
    }
}
