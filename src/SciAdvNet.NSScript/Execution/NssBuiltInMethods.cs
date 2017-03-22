using System;

namespace SciAdvNet.NSScript.Execution
{
    public class NssBuiltInMethods : INssBuiltInMethods
    {
        public uint CallingThreadId { get; }

        public virtual void CreateChoice(string entityName)
        {
        }

        public virtual void RemoveEntity(string entityName)
        {
        }


        public virtual void DrawTransition(int time, int start, int end, int unk, string filename, bool wait)
        {
        }

        public virtual void Fade(string entityName, TimeSpan duration, int opacity, bool wait)
        {
        }

        public virtual void LoadAudio(string entityName, AudioKind kind, string fileName)
        {
        }

        public virtual void PlayCutscene(string entityName, int priority, bool loop, bool alpha, string fileName, bool enableAudio)
        {
        }

        public virtual void LoadImage(string entityName, string fileName)
        {
        }

        public virtual void Request(string entityName, NssAction action)
        {
        }

        public virtual void SetAlias(string entityName, string alias)
        {
        }

        public virtual void SetLoop(string entityName, bool loop)
        {
        }

        public virtual void SetLoopPoint(string entityName, TimeSpan loopStart, TimeSpan loopEnd)
        {
        }

        public virtual void SetVolume(string entityName, TimeSpan duration, int volume)
        {
        }

        public virtual void Wait(TimeSpan delay)
        {
        }

        public virtual void WaitForInput()
        {
        }

        public virtual void WaitForInput(TimeSpan timeout)
        {
        }

        public virtual void WaitText(string id, TimeSpan time)
        {
        }

        public virtual void DisplayDialogue(DialogueLine dialogue)
        {
        }

        public virtual void CreateDialogueBox(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height)
        {
        }

        public virtual void AddText(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height, string text)
        {
        }

        public virtual void AddTexture(string entityName, int priority, NssCoordinate x, NssCoordinate y, string fileOrEntityName)
        {
        }

        public virtual void AddRectangle(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color)
        {
        }

        public virtual void Move(string entityName, TimeSpan duration, NssCoordinate x, NssCoordinate y, bool wait)
        {
        }

        public virtual void Zoom(string entityName, TimeSpan duration, NssCoordinate x, NssCoordinate y, bool wait)
        {
        }
    }
}
