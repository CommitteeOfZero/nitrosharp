using System;
using System.Collections.Immutable;

namespace SciAdvNet.NSScript.Execution
{
    public class NssBuildInMethods : INssBuiltInMethods
    {
        public virtual void CreateChoice(string objectName)
        {
        }

        public virtual void RemoveObject(string objectName)
        {
        }


        public virtual void DrawTransition(int time, int start, int end, int unk, string filename, bool wait)
        {
        }

        public virtual void FadeIn(string objectName, TimeSpan duration, int opacity, bool wait)
        {
        }

        public virtual void LoadAudio(string objectName, AudioKind kind, string fileName)
        {
        }

        public virtual void PlayCutscene(string objectName, int zLevel, bool loop, bool alpha, string fileName, bool enableAudio)
        {
        }

        public virtual void LoadImage(string objectName, string fileName)
        {
        }

        public virtual void Request(string objectName, NssAction action)
        {
        }

        public virtual void SetAlias(string objectName, string alias)
        {
        }

        public virtual void SetLoop(string objectName, bool loop)
        {
        }

        public virtual void SetLoopPoint(string objectName, TimeSpan loopStart, TimeSpan loopEnd)
        {
        }

        public virtual void SetVolume(string objectName, TimeSpan duration, int volume)
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

        public virtual void CreateDialogueBox(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height)
        {
        }

        public virtual void AddText(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height, string text)
        {
        }

        public virtual void AddTexture(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, string fileOrObjectName)
        {
        }

        public virtual void AddRectangle(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color)
        {
        }

        public virtual void Move(string objectName, TimeSpan duration, NssCoordinate x, NssCoordinate y, bool wait)
        {
        }

        public virtual void Zoom(string objectName, TimeSpan duration, NssCoordinate x, NssCoordinate y, bool wait)
        {
        }
    }
}
