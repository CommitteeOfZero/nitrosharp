using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpDX.D3DCompiler;
using Veldrid;
using Veldrid.SPIRV;

namespace NitroSharp.ShaderCompiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                try
                {
                    CompileAll(args[0]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static void CompileAll(string inputDirectory)
        {
            string outputDirectory = Path.Combine(inputDirectory, "Generated");
            Directory.CreateDirectory(outputDirectory);
            IEnumerable<string> files = Directory.EnumerateFiles(inputDirectory);

            var shaderSets =
                from path in files
                let vert = path.EndsWith("vert")
                let frag = path.EndsWith("frag")
                where vert || frag
                let name = Path.GetFileNameWithoutExtension(path)
                group (path, vert, frag) by name into g
                where g.Count() == 2
                select new
                {
                    Name = g.Key,
                    Vertex = g.FirstOrDefault(x => x.vert).path,
                    Fragment = g.FirstOrDefault(x => x.frag).path,
                };

            foreach (var shaderSet in shaderSets)
            {
                string outputBase = Path.Combine(outputDirectory, shaderSet.Name);
                byte[] vs = File.ReadAllBytes(shaderSet.Vertex);
                byte[] fs = File.ReadAllBytes(shaderSet.Fragment);

                string vsSource = Encoding.UTF8.GetString(vs);
                string fsSource = Encoding.UTF8.GetString(fs);

                var debugCompileOptions = new GlslCompileOptions(debug: true);
                var vsSpvDebugOutput = SpirvCompilation.CompileGlslToSpirv(
                    vsSource, string.Empty, ShaderStages.Vertex,
                    debugCompileOptions);
                var fsSpvDebugOutput = SpirvCompilation.CompileGlslToSpirv(
                    fsSource, string.Empty, ShaderStages.Fragment,
                    debugCompileOptions);

                var releaseCompileOptions = new GlslCompileOptions(debug: false);
                var vsSpvReleaseOutput = SpirvCompilation.CompileGlslToSpirv(
                    vsSource, string.Empty, ShaderStages.Vertex,
                    releaseCompileOptions);
                var fsSpvReleaseOutput = SpirvCompilation.CompileGlslToSpirv(
                    fsSource, string.Empty, ShaderStages.Fragment,
                    releaseCompileOptions);
                File.WriteAllBytes(outputBase + "-vertex.450.glsl.spv", vsSpvReleaseOutput.SpirvBytes);
                File.WriteAllBytes(outputBase + "-fragment.450.glsl.spv", fsSpvDebugOutput.SpirvBytes);

                var glCompileOptions = new CrossCompileOptions(fixClipSpaceZ: true, invertVertexOutputY: false);
                var glslResult = SpirvCompilation.CompileVertexFragment(
                    vsSpvDebugOutput.SpirvBytes,
                    fsSpvDebugOutput.SpirvBytes,
                    CrossCompileTarget.GLSL,
                    glCompileOptions);
                File.WriteAllText(outputBase + "-vertex.330.glsl", glslResult.VertexShader);
                File.WriteAllText(outputBase + "-fragment.330.glsl", glslResult.FragmentShader);

                var esslResult = SpirvCompilation.CompileVertexFragment(
                    vsSpvDebugOutput.SpirvBytes,
                    fsSpvDebugOutput.SpirvBytes,
                    CrossCompileTarget.ESSL,
                    glCompileOptions);
                File.WriteAllText(outputBase + "-vertex.300.glsles", glslResult.VertexShader);
                File.WriteAllText(outputBase + "-fragment.300.glsles", glslResult.FragmentShader);

                var hlslDebugOutput = SpirvCompilation.CompileVertexFragment(
                    vsSpvDebugOutput.SpirvBytes,
                    fsSpvDebugOutput.SpirvBytes,
                    CrossCompileTarget.HLSL);
                File.WriteAllText(outputBase + "-vertex.hlsl", hlslDebugOutput.VertexShader);
                File.WriteAllText(outputBase + "-fragment.hlsl", hlslDebugOutput.FragmentShader);

                var hlslReleaseOutput = SpirvCompilation.CompileVertexFragment(
                    vsSpvReleaseOutput.SpirvBytes,
                    fsSpvReleaseOutput.SpirvBytes,
                    CrossCompileTarget.HLSL);

                byte[] vertBytes = Encoding.UTF8.GetBytes(hlslReleaseOutput.VertexShader);
                byte[] fragBytes = Encoding.UTF8.GetBytes(hlslReleaseOutput.FragmentShader);
                File.WriteAllBytes(outputBase + "-vertex.hlsl.bytes", CompileHlsl(ShaderStages.Vertex, vertBytes));
                File.WriteAllBytes(outputBase + "-fragment.hlsl.bytes", CompileHlsl(ShaderStages.Fragment, fragBytes));
            }
        }

        private static byte[] CompileHlsl(ShaderStages stage, byte[] sourceCode)
        {
            string profile = stage == ShaderStages.Vertex ? "vs_5_0" : "ps_5_0";

            ShaderFlags flags = ShaderFlags.OptimizationLevel3;
            CompilationResult result = ShaderBytecode.Compile(
                sourceCode,
                "main",
                profile,
                flags);

            if (result.ResultCode.Failure || result.Bytecode == null)
            {
                Console.WriteLine($"Failed to compile HLSL code: {result.Message}");
                return Array.Empty<byte>();
            }

            return result.Bytecode.Data;
        }
    }
}
