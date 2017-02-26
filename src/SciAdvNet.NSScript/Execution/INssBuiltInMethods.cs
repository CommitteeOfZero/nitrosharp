using System;

namespace SciAdvNet.NSScript.Execution
{
    public interface INssBuiltInMethods
    {
        void DisplayDialogue(DialogueLine dialogue);

        void SetAlias(string objectName, string alias);
        void Wait(TimeSpan delay);

        /// <summary>
        /// Original name: WaitKey
        /// </summary>
        void WaitForInput();
        /// <summary>
        /// Original name: WaitKey
        /// </summary>
        void WaitForInput(TimeSpan timeout);

        void LoadImage(string objectName, string fileName);

        /// <summary>
        /// Original name: CreateSound.
        /// </summary>
        void LoadAudio(string objectName, AudioKind kind, string fileName);

        void CreateDialogueBox(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height);

        /// <summary>
        /// Original name: CreateText.
        /// </summary>
        void AddText(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height, string text);

        void WaitText(string id, TimeSpan time);

        /// <summary>
        /// Loads and displays an image at the specified coordinates.
        /// Original name: CreateTexture.
        /// </summary>
        void AddTexture(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, string fileOrObjectName);

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        void AddRectangle(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color);

        
        void Request(string objectName, NssAction action);
        void RemoveObject(string objectName);

        void FadeIn(string objectName, TimeSpan duration, int finalOpacity, bool wait);
        void Move(string objectName, TimeSpan duration, NssCoordinate x, NssCoordinate y, bool wait);
        void Zoom(string objectName, TimeSpan duration, NssCoordinate x, NssCoordinate y, bool wait);

        void SetLoopPoint(string objectName, TimeSpan loopStart, TimeSpan loopEnd);
        void SetLoop(string objectName, bool loop);
        void SetVolume(string objectName, TimeSpan duration, int volume);

        void CreateChoice(string objectName);
        void PlayCutscene(string objectName, int zLevel, bool loop, bool alpha, string fileName, bool enableAudio);
        void DrawTransition(int time, int start, int end, int unk, string filename, bool wait);
    }
}
