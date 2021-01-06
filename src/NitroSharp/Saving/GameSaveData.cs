using NitroSharp.NsScript.VM;

namespace NitroSharp.Saving
{
    [Persistable]
    internal readonly partial struct CommonSaveData
    {
        public GlobalsDump Flags { get; init; }
    }

    [Persistable]
    internal readonly partial struct GameSaveData
    {
        public GameProcessSaveData MainProcess { get; init; }
        public GlobalsDump Variables { get; init; }
    }
}
