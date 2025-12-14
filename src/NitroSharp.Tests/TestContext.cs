using NitroSharp.Text;

namespace NitroSharp.Tests;

internal class TestContext
{
    static TestContext()
    {
        MainProcess = new(EntityName.Parse("test"), null, new FontSettings());
        MainThread = new Thread(EntityName.Parse("main"), MainProcess, default, isMain: true);
    }

    public static Process MainProcess { get; }
    public static Thread MainThread { get; }
}
