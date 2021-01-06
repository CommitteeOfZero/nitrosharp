using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MessagePack;
using NitroSharp.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Processing;

namespace NitroSharp.Saving
{
    internal class GameSaveManager
    {
        private const uint AutosaveSlot = 9999;

        private readonly string _rootDir;
        private readonly string _commonDir;

        public GameSaveManager(Configuration configuration)
        {
            _rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.Combine("Committee of Zero", configuration.ProductName)
            );
            _rootDir = Path.Combine(_rootDir, configuration.ProfileName);
            Directory.CreateDirectory(_rootDir);
            _commonDir = Path.Combine(_rootDir, "common");
            Directory.CreateDirectory(_commonDir);
        }

        public string SaveDirectory => _rootDir;

        public bool SaveExists(uint slot)
        {
            string saveDir = GetSaveDirectory(slot, out string savenum);
            string savePath = Path.Combine(saveDir, $"{savenum}.sav");
            return File.Exists(savePath);
        }

        public void Save(GameContext ctx, uint slot)
        {
            if (slot == 0)
            {
                WriteCommonSaveData(ctx);
                return;
            }

            var savingCtx = new GameSavingContext(ctx.MainProcess.World);
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(buffer);

            RenderContext rc = ctx.RenderContext;
            CommandList cl = rc.RentCommandList();
            cl.Begin();
            GameProcessSaveData process = ctx.MainProcess.Dump(savingCtx);
            Texture[] standaloneTextures = savingCtx.StandaloneTextures
                .Select(x => rc.ReadbackTexture(cl, x))
                .ToArray();
            cl.End();
            Fence fence = rc.ResourceFactory.CreateFence(signaled: false);
            rc.GraphicsDevice.SubmitCommands(cl, fence);
            rc.GraphicsDevice.WaitForFence(fence);
            rc.ReturnCommandList(cl);

            var saveData = new GameSaveData
            {
                Variables = ctx.VM.DumpVariables(),
                MainProcess = process
            };
            saveData.Serialize(ref writer);
            writer.Flush();

            string saveDir = GetSaveDirectory(slot, out string savenum);
            Directory.CreateDirectory(saveDir);
            string savePath = Path.Combine(saveDir, $"{savenum}.sav");
            using FileStream saveFile = File.OpenWrite(savePath);
            saveFile.Write(buffer.WrittenSpan);

            for (int i = 0; i < standaloneTextures.Length; i++)
            {
                Texture texture = standaloneTextures[i];
                using (FileStream fileStream = File.Create(Path.Combine(saveDir, $"{i:D4}.png")))
                {
                    SaveAsPng(rc, texture, fileStream, texture.Width, texture.Height);
                }
            }

            if (slot != AutosaveSlot)
            {
                Debug.Assert(ctx.LastScreenshot is not null);
                using FileStream thumbStream = File.Create(Path.Combine(saveDir, "thum.npf"));
                SaveAsPng(ctx.RenderContext, ctx.LastScreenshot, thumbStream, 128, 72);
            }
        }

        public void Load(GameContext ctx, uint slot)
        {
            if (slot == 0)
            {
                ReadCommonSaveData(ctx);
                return;
            }

            string saveDir = GetSaveDirectory(slot, out string savenum);
            string savePath = Path.Combine(saveDir, $"{savenum}.sav");
            byte[] bytes = File.ReadAllBytes(savePath);
            var reader = new MessagePackReader(bytes);
            var save = new GameSaveData(ref reader);

            int i = 0;
            string texPath;
            RenderContext rc = ctx.RenderContext;
            var standaloneTextures = new List<Texture>();
            while (File.Exists(texPath = Path.Combine(saveDir, $"{i:D4}.png")))
            {
                Texture tex = rc.Content.LoadTexture(texPath, staging: false);
                standaloneTextures.Add(tex);
                i++;
            }

            ctx.MainProcess.Dispose();
            ctx.SysProcess = null;

            ctx.VM.RestoreVariables(save.Variables);
            ctx.MainProcess = new GameProcess(ctx, save.MainProcess, standaloneTextures);
        }

        private void WriteCommonSaveData(GameContext ctx)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(buffer);
            var data = new CommonSaveData
            {
                Flags = ctx.VM.DumpFlags()
            };
            data.Serialize(ref writer);
            writer.Flush();

            string filePath = Path.Combine(_commonDir, "val.npf");
            using FileStream file = File.OpenWrite(filePath);
            file.Write(buffer.WrittenSpan);
        }

        public void ReadCommonSaveData(GameContext ctx)
        {
            string filePath = Path.Combine(_commonDir, "val.npf");
            if (!File.Exists(filePath)) { return; }
            byte[] bytes = File.ReadAllBytes(filePath);
            var reader = new MessagePackReader(bytes);
            var saveData = new CommonSaveData(ref reader);
            ctx.VM.RestoreFlags(saveData.Flags);
        }

        private string GetSaveDirectory(uint slot, out string savenum)
        {
            savenum = $"{slot:D4}";
            string dir = Path.Combine(_rootDir, savenum);
            return dir;
        }

        private unsafe void SaveAsPng(
            RenderContext ctx,
            Texture texture,
            Stream dstStream,
            uint dstWidth, uint dstHeight)
        {
            MappedResource map = ctx.GraphicsDevice.Map(texture, MapMode.Read);
            var span = new ReadOnlySpan<Bgra32>(map.Data.ToPointer(), (int)map.SizeInBytes / 4);
            Image<Bgra32> image = Image.LoadPixelData(span, (int)texture.Width, (int)texture.Height);
            ctx.GraphicsDevice.Unmap(texture);
            if (dstWidth != texture.Width || dstHeight != texture.Height)
            {
                image.Mutate(x => x.Resize((int)dstWidth, (int)dstHeight));
            }
            image.SaveAsPng(dstStream);
        }
    }
}
