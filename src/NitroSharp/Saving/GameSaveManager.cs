using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MessagePack;
using NitroSharp.Graphics;
using NitroSharp.Graphics.Core;
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

        private readonly string _commonDir;

        public GameSaveManager(GameProfile gameProfile)
        {
            string localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create
            );
            SaveDirectory = Path.Combine(
                localAppData,
                Path.Combine("Committee of Zero", gameProfile.ProductName)
            );
            SaveDirectory = Path.Combine(SaveDirectory, gameProfile.Name);
            Directory.CreateDirectory(SaveDirectory);
            _commonDir = Path.Combine(SaveDirectory, "common");
            Directory.CreateDirectory(_commonDir);
        }

        public string SaveDirectory { get; }

        public bool CommonSaveDataExists()
        {
            return File.Exists(Path.Combine(_commonDir, "val.npf"));
        }

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
            CommandList cl = rc.CommandListPool.Rent();
            cl.Begin();
            GameProcessSaveData process = ctx.MainProcess.Dump(savingCtx);
            Texture[] standaloneTextures = savingCtx.StandaloneTextures
                .Select(x => rc.ReadbackTexture(cl, x))
                .ToArray();
            cl.End();
            Fence fence = rc.ResourceFactory.CreateFence(signaled: false);
            rc.GraphicsDevice.SubmitCommands(cl, fence);
            rc.GraphicsDevice.WaitForFence(fence);

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
                using (Texture texture = standaloneTextures[i])
                using (FileStream fileStream = File.Create(Path.Combine(saveDir, $"{i:D4}.png")))
                {
                    SaveAsPng(rc, texture, fileStream, texture.Width, texture.Height);
                }
            }

            if (slot != AutosaveSlot)
            {
                using FileStream thumbStream = File.Create(Path.Combine(saveDir, "thum.npf"));
                using PooledTexture gpuScreenshot = ctx.RenderToTexture(ctx.MainProcess);
                cl.Begin();
                using Texture screenshot = rc.ReadbackTexture(cl, gpuScreenshot.Get());
                cl.End();
                fence.Reset();
                rc.GraphicsDevice.SubmitCommands(cl, fence);
                rc.GraphicsDevice.WaitForFence(fence);
                SaveAsPng(ctx.RenderContext, screenshot, thumbStream, 128, 72);

                File.Create(Path.Combine(saveDir, "val.npf")).Close();
                File.Create(Path.Combine(saveDir, "script.npf")).Close();
                File.Create(Path.Combine(saveDir, "frames.npf")).Close();
                File.Create(Path.Combine(saveDir, "bklg.npf")).Close();
            }

            rc.CommandListPool.Return(cl);
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
            File.Create(Path.Combine(_commonDir, "cqst.npf")).Close();
        }

        private void ReadCommonSaveData(GameContext ctx)
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
            string dir = Path.Combine(SaveDirectory, savenum);
            return dir;
        }

        private static unsafe void SaveAsPng(
            RenderContext ctx,
            Texture texture,
            Stream dstStream,
            uint dstWidth, uint dstHeight)
        {
            MappedResource map = ctx.GraphicsDevice.Map(texture, MapMode.Read);
            var span = new ReadOnlySpan<Bgra32>(map.Data.ToPointer(), (int)map.SizeInBytes / 4);
            var image = Image.LoadPixelData(span, (int)texture.Width, (int)texture.Height);
            ctx.GraphicsDevice.Unmap(texture);
            if (dstWidth != texture.Width || dstHeight != texture.Height)
            {
                image.Mutate(x => x.Resize((int)dstWidth, (int)dstHeight));
            }
            image.SaveAsPng(dstStream);
        }
    }
}
