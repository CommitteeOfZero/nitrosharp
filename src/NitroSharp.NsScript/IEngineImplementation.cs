using System;

namespace NitroSharp.NsScript
{
    public interface IEngineImplementation
    {
        int GetPlatformId();
        string GetCurrentModuleName();
        int GenerateRandomNumber(int max);

        int GetSoundAmplitude(string characterName);
        int GetTextureHeight(string entityName);
        int GetTextureWidth(string entityName);


        /// <summary>
        /// Original name: DurationTime.
        /// </summary>
        int GetSoundDuration(string entityName);

        /// <summary>
        /// Original name: PassageTime.
        /// </summary>
        int GetTimeElapsed(string entityName);

        /// <summary>
        /// Original name: RemainTime.
        int GetTimeRemaining(string soundEntityName);

        void SetAlias(string entityName, string alias);
        void Request(string entityName, NsEntityAction action);
        void RemoveEntity(string entityName);

        /// <summary>
        /// Original name: Wait.
        /// </summary>
        /// <param name="delay"></param>
        void Delay(TimeSpan delay);

        /// <summary>
        /// Original name: WaitKey.
        /// </summary>
        void WaitForInput();

        void WaitForInput(TimeSpan timeout);

        void WaitText(string id, TimeSpan time);

        /// <summary>
        /// Original name: CreateWindow.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="priority"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        void CreateDialogueBox(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height);

        /// <summary>
        /// Original name: CreateText.
        /// </summary>
        void AddText(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, string text);

        /// <summary>
        /// Original name: CreateTexture.
        /// </summary>
        void AddTexture(string entityName, int priority, NsCoordinate x, NsCoordinate y, string fileOrExistingEntityName);

        /// <summary>
        /// Original name: CreateColor.
        /// </summary>
        void AddRectangle(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, NsColor color);

        /// <summary>
        /// Original name: CreateSound.
        /// </summary>
        void LoadAudio(string entityName, NsAudioKind kind, string fileName);

        void LoadImage(string entityName, string fileName);

        /// <summary>
        /// Original name: SetLoop.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="looping"></param>
        void ToggleLooping(string entityName, bool looping);

        void SetLoopPoint(string entityName, TimeSpan loopStart, TimeSpan loopEnd);
        void SetVolume(string entityName, TimeSpan duration, NsRational volume);
        void Fade(string entityName, TimeSpan duration, NsRational opacity, bool wait);
        void Move(string entityName, TimeSpan duration, NsCoordinate x, NsCoordinate y, NsEasingFunction easingFunction, bool wait);
        void CreateThread(string name, string target);
        void Zoom(string entityName, TimeSpan duration, NsRational scaleX, NsRational scaleY, NsEasingFunction easingFunction, bool wait);
        void DrawTransition(string sourceEntityName, TimeSpan duration, NsRational initialOpacity, NsRational finalOpacity, NsRational feather, string maskFileName, bool wait);
        void CreateChoice(string entityName);
        void PlayCutscene(string entityName, int priority, bool loop, bool alpha, string fileName, bool enableAudio);
        void AddClippedTexture(string entityName, int priority, NsCoordinate x1, NsCoordinate y1, NsCoordinate x2, NsCoordinate y2, int width, int height, string srcEntityName);
    }
}
