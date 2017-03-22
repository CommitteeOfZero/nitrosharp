using System;

namespace SciAdvNet.NSScript.Execution
{
    public interface INssBuiltInMethods
    {
        uint CallingThreadId { get; }

        void DisplayDialogue(DialogueLine dialogue);

        void SetAlias(string entityName, string alias);
        void Wait(TimeSpan delay);

        /// <summary>
        /// Original name: WaitKey
        /// </summary>
        void WaitForInput();
        /// <summary>
        /// Original name: WaitKey
        /// </summary>
        void WaitForInput(TimeSpan timeout);

        void LoadImage(string entityName, string fileName);

        /// <summary>
        /// Original name: CreateSound.
        /// </summary>
        void LoadAudio(string entityName, AudioKind kind, string fileName);

        void CreateDialogueBox(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height);

        /// <summary>
        /// Original name: CreateText.
        /// </summary>
        void AddText(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height, string text);

        void WaitText(string id, TimeSpan time);

        /// <summary>
        /// Loads and displays an image at the specified coordinates.
        /// Original name: CreateTexture.
        /// </summary>
        void AddTexture(string entityName, int priority, NssCoordinate x, NssCoordinate y, string fileOrEntityName);

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        void AddRectangle(string entityName, int priority, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color);

        
        void Request(string entityName, NssAction action);
        void RemoveEntity(string entityName);

        void Fade(string entityName, TimeSpan duration, int finalOpacity, bool wait);
        void Move(string entityName, TimeSpan duration, NssCoordinate x, NssCoordinate y, bool wait);
        void Zoom(string entityName, TimeSpan duration, NssCoordinate x, NssCoordinate y, bool wait);

        void SetLoopPoint(string entityName, TimeSpan loopStart, TimeSpan loopEnd);
        void SetLoop(string entityName, bool looping);
        void SetVolume(string entityName, TimeSpan duration, int volume);

        void CreateChoice(string entityName);
        void PlayCutscene(string entityName, int priority, bool loop, bool alpha, string fileName, bool enableAudio);
        void DrawTransition(int time, int start, int end, int unk, string filename, bool wait);
    }
}
