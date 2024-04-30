using NitroSharp.NsScript.VM;

namespace NitroSharp;

internal sealed class Thread : Entity, IVmThread
{
    private NsScriptThreadState _vmState;

    public Thread(EntityName name, Entity? parent, NsScriptThreadState vmState, bool isMain)
        : base(name, parent)
    {
        _vmState = vmState;
        IsMain = isMain;
    }

    public bool IsMain { get; }
    public ref NsScriptThreadState State => ref _vmState;

    public override void Update(GameContext ctx)
    {
        ctx.VM.Tick(ref _vmState, ctx.Builtins);
        foreach (Entity entity in GetDescendants())
        {
            entity.Update(ctx);
        }
    }
}
