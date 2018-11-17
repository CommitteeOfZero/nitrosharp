namespace NitroSharp
{
    internal readonly struct InterpreterThreadInfo
    {
        public InterpreterThreadInfo(string name, string module, string target)
        {
            Name = name;
            Module = module;
            Target = target;
        }

        public readonly string Name;
        public readonly string Module;
        public readonly string Target;
    }
}
