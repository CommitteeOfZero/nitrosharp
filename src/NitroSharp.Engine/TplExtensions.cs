using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NitroSharp
{
    public static class TplExtensions
    {
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this Task task)
        {
        }
    }
}
