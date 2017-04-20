using System;

namespace CommitteeOfZero.NsScript.Execution
{
    public abstract class BuiltInFunctionsBase
    {
        public NsScriptInterpreter Interpreter { get; private set; }
        public ThreadContext MainThread { get; internal set; }
        public ThreadContext CurrentThread { get; internal set; }
        public DialogueBlock CurrentDialogueBlock { get; internal set; }

        public event EventHandler<DialogueBlock> EnteredDialogueBlock;

        internal void SetInterpreter(NsScriptInterpreter instance) => Interpreter = instance;

        internal void RaiseEnteredDialogueBlock(DialogueBlock block)
        {
            EnteredDialogueBlock?.Invoke(this, block);
        }

        public virtual int GetTextureWidth(string fileName)
        {
            return 0;
        }

        public virtual void DisplayDialogue(string pxmlString)
        {
        }

        public virtual void SetAlias(string entityName, string alias)
        {
        }

        public virtual void Request(string entityName, NsEntityAction action)
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
            CurrentThread.PushContinuation(CurrentThread.CurrentFrame.Function, CurrentDialogueBlock);
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
        public virtual void CreateDialogueBox(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height)
        {
        }

        /// <summary>
        /// Original name: CreateText.
        /// </summary>
        public virtual void AddText(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, string text)
        {
        }

        /// <summary>
        /// Original name: CreateTexture.
        /// </summary>
        public virtual void AddTexture(string entityName, int priority, NsCoordinate x, NsCoordinate y, string fileOrExistingEntityName)
        {
        }

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        public virtual void AddRectangle(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, NsColor color)
        {
        }

        /// <summary>
        /// Original name: CreateSound.
        /// </summary>
        public virtual void LoadAudio(string entityName, NsAudioKind kind, string fileName)
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

        public virtual void Fade(string entityName, TimeSpan duration, NsRational opacity, bool wait)
        {
        }

        public virtual void Move(string entityName, TimeSpan duration, NsCoordinate x, NsCoordinate y, NsEasingFunction easingFunction, bool wait)
        {
        }

        public virtual void CreateThread(string name, string target)
        {
            Interpreter.CreateThread(CurrentThread.CurrentModule, target);
        }

        public virtual void Zoom(string entityName, TimeSpan duration, NsRational scaleX, NsRational scaleY, NsEasingFunction easingFunction, bool wait)
        {
        }

        public virtual void DrawTransition(string entityName, TimeSpan duration, NsRational initialOpacity, NsRational finalOpacity, NsRational feather, string fileName, bool wait)
        {
        }

        public virtual void CreateChoice(string entityName)
        {
        }

        public virtual void PlayCutscene(string entityName, int priority, bool loop, bool alpha, string fileName, bool enableAudio)
        {
        }

        public virtual void AddClippedTexture(string entityName, int priority, NsCoordinate x1, NsCoordinate y1, NsCoordinate x2, NsCoordinate y2, int width, int height, string srcEntityName)
        {
        }

        public virtual int GetSoundAmplitude()
        {
            return 0;
        }
    }
}
