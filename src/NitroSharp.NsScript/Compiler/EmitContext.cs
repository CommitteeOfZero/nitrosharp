using System.Collections.Generic;

namespace NitroSharp.NsScript.Compiler;

internal sealed class EmitContext(Compilation compilation)
{
    public Compilation Compilation { get; } = compilation;
    public Dictionary<ResolvedPath, NsxModuleBuilder> NsxModuleBuilders { get; } = new();
    public TokenMap<string> Variables { get; } = new(4096);
    public TokenMap<string> Flags { get; } = new(256);
    public List<string> SystemVariables { get; } = [];
    public List<string> SystemFlags { get; } = [];
    public DiagnosticBuilder DiagnosticBuilder { get; } = new();

    public NsxModuleBuilder GetNsxModuleBuilder(SourceFileSymbol sourceFile)
    {
        if (!NsxModuleBuilders.TryGetValue(sourceFile.FilePath, out NsxModuleBuilder? moduleBuilder))
        {
            moduleBuilder = new NsxModuleBuilder(this, sourceFile);
            NsxModuleBuilders.Add(sourceFile.FilePath, moduleBuilder);
        }

        return moduleBuilder;
    }

    public ushort GetVariableToken(string name)
    {
        if (!Variables.TryGetToken(name, out ushort token))
        {
            token = Variables.AddToken(name);
            if (name.StartsWith("SYSTEM"))
            {
                SystemVariables.Add(name);
            }
        }

        return token;
    }

    public ushort GetFlagToken(string name)
    {
        if (!Flags.TryGetToken(name, out ushort token))
        {
            token = Flags.AddToken(name);
            if (name.StartsWith("SYSTEM"))
            {
                SystemFlags.Add(name);
            }
        }

        return token;
    }

    public bool TryGetVariableToken(string variableName, out ushort token)
        => Variables.TryGetToken(variableName, out token);
}
