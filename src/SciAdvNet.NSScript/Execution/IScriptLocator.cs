using System.IO;

namespace SciAdvNet.NSScript.Execution
{
    public interface IScriptLocator
    {
        Stream Locate(string fileName);
    }
}
