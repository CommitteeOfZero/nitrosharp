using System.IO;

namespace CommitteeOfZero.NsScript.Execution
{
    public interface IScriptLocator
    {
        Stream Locate(string fileName);
    }
}
