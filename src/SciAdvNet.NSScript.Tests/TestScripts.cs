using System.IO;
using System.Reflection;
using System.Text;

namespace SciAdvNet.NSScript.Tests
{
    public class TestScripts
    {
        static TestScripts()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static string Get(string scriptName)
        {
            string path = Path.Combine("Data", scriptName);
            return File.ReadAllText(path);
            //var assembly = typeof(TestScripts).GetTypeInfo().Assembly;
            //string resourceNamespace = typeof(TestScripts).Namespace;
            //string fullName = $"{resourceNamespace}.Data.{scriptName}";
            //using (var stream = assembly.GetManifestResourceStream(fullName))
            //using (var reader = new StreamReader(stream, Encoding.GetEncoding("shift-jis")))
            //{
            //    string contents = reader.ReadToEnd();
            //    return contents;
            //}
        }
    }
}
