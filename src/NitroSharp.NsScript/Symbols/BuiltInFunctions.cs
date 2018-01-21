using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NitroSharp.NsScript.Symbols
{
    public static class BuiltInFunctions
    {
        private const int NssMaxOpacity = 1000;
        private const int NssMaxVolume = 1000;

        public static SymbolTable Symbols { get; }

        static BuiltInFunctions()
        {
            Symbols = new SymbolTable();
            Declare("Wait", Wait);
            Declare("WaitKey", WaitKey);
            Declare("Request", Request);
            Declare("SetAlias", SetAlias);
            Declare("Delete", Delete);
            Declare("CreateProcess", CreateProcess);
            Declare("LoadImage", LoadImage);
            Declare("CreateColor", CreateColor);
            Declare("CreateTexture", CreateTexture);
            Declare("CreateClipTexture", CreateClipTexture);
            Declare("CreateSound", CreateSound);
            Declare("Fade", Fade);
            Declare("Move", Move);
            Declare("Zoom", Zoom);
            Declare("SetVolume", SetVolume);
            Declare("CreateWindow", CreateWindow);
            Declare("LoadText", LoadText);
            Declare("WaitText", WaitText);
            Declare("SetLoop", SetLoop);
            Declare("SetLoopPoint", SetLoopPoint);
            Declare("DrawTransition", DrawTransition);

            Declare("DurationTime", DurationTime);
            Declare("PassageTime", PassageTime);
            Declare("RemainTime", RemainTime);
            Declare("ImageHorizon", ImageHorizon);
            Declare("ImageVertical", ImageVertical);
            Declare("Random", Random);
            Declare("SoundAmplitude", SoundAmplitude);
            Declare("Platform", Platform);
            Declare("ModuleFileName", ModuleFileName);
            Declare("String", String);
            Declare("Time", Time);
        }

        private static ConstantValue Time(EngineImplementationBase arg1, Stack<ConstantValue> arg2)
        {
            return ConstantValue.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Declare(string functionName, Func<EngineImplementationBase, Stack<ConstantValue>, ConstantValue> implementation)
        {
            var symbol = new BuiltInFunctionSymbol(functionName, implementation);
            Symbols.Declare(symbol);
        }

        private static ConstantValue String(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string format = PopString(args);

            var list = new List<int>();
            while (args.Count > 0)
            {
                list.Add((int)PopDouble(args, allowNull: false, allowTypeConversion: true));
            }

            var builder = new StringBuilder();
            swprintf(builder, format, list[0]);
            return ConstantValue.Create(builder.ToString());
        }

        [DllImport("msvcrt.Dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        static extern int swprintf([In, Out]StringBuilder buffer, String fmt, int arg1);


        private static ConstantValue ModuleFileName(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            return ConstantValue.Create(implementation.GetCurrentModuleName());
        }

        private static ConstantValue Platform(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            return ConstantValue.Create(implementation.GetPlatformId());
        }

        private static ConstantValue ImageHorizon(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            int width = implementation.GetTextureWidth(entityName);
            return ConstantValue.Create(width);
        }

        private static ConstantValue ImageVertical(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            int height = implementation.GetTextureHeight(entityName);
            return ConstantValue.Create(height);
        }

        private static ConstantValue DurationTime(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            int msTime = implementation.GetSoundDuration(entityName);
            return ConstantValue.Create(msTime);
        }

        private static ConstantValue PassageTime(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            int msTime = implementation.GetTimeElapsed(entityName);
            return ConstantValue.Create(msTime);
        }

        private static ConstantValue RemainTime(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            int msTime = implementation.GetTimeRemaining(entityName);
            return ConstantValue.Create(msTime);
        }

        private static ConstantValue SoundAmplitude(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string unk = PopString(args);
            string characterName = PopString(args);

            int amplitude = implementation.GetSoundAmplitude(characterName);
            return ConstantValue.Create(amplitude);
        }

        private static ConstantValue Random(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            int max = PopDouble(args);
            int n = implementation.GenerateRandomNumber(max);
            return ConstantValue.Create(n);
        }

        private static ConstantValue Wait(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            TimeSpan delay = PopTimeSpan(args, allowNull: true);
            implementation.Delay(delay);
            return null;
        }

        private static ConstantValue WaitKey(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            if (args.Count > 0 && args.Peek().Type == BuiltInType.Double)
            {
                TimeSpan timeout = PopTimeSpan(args);
                implementation.WaitForInput(timeout);
            }
            else
            {
                implementation.WaitForInput();
            }

            return null;
        }

        private static ConstantValue SetAlias(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            string alias = EntityName(PopString(args));

            implementation.SetAlias(entityName, alias);
            return null;
        }

        private static ConstantValue CreateProcess(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string name = PopString(args);
            args.Pop();
            args.Pop();
            args.Pop();
            string target = PopString(args);

            implementation.CreateThread(name, target);
            return null;
        }

        private static ConstantValue Request(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            NsEntityAction action = EnumConversions.ToEntityAction(PopEnumValue(args));

            implementation.Request(entityName, action);
            return null;
        }

        private static ConstantValue Delete(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            implementation.RemoveEntity(entityName);
            return null;
        }

        private static ConstantValue LoadImage(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            string fileName = PopString(args);

            implementation.LoadImage(entityName, fileName);
            return null;
        }

        private static ConstantValue CreateTexture(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            int priority = PopDouble(args);
            NsCoordinate x = PopCoordinate(args);
            NsCoordinate y = PopCoordinate(args);
            string fileOrEntityName = EntityName(PopString(args, allowNull: false, allowTypeConversion: true));

            implementation.AddTexture(entityName, priority, x, y, fileOrEntityName);
            return null;
        }

        private static ConstantValue CreateClipTexture(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            int priority = PopDouble(args);
            NsCoordinate x1 = PopCoordinate(args);
            NsCoordinate y1 = PopCoordinate(args);
            NsCoordinate x2 = PopCoordinate(args);
            NsCoordinate y2 = PopCoordinate(args);
            int width = PopDouble(args);
            int height = PopDouble(args);
            string srcEntityName = PopString(args);

            implementation.AddClippedTexture(entityName, priority, x1, y1, x2, y2, width, height, srcEntityName);
            return null;
        }

        private static ConstantValue CreateSound(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            NsAudioKind kind = EnumConversions.ToAudioKind(PopEnumValue(args));
            string fileName = PopString(args);
            implementation.LoadAudio(entityName, kind, fileName);
            return null;
        }

        private static ConstantValue CreateColor(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            int priority = PopDouble(args);
            NsCoordinate x = PopCoordinate(args);
            NsCoordinate y = PopCoordinate(args);
            int width = PopDouble(args);
            int height = PopDouble(args);
            NsColor color = PopColor(args);

            implementation.AddRectangle(entityName, priority, x, y, width, height, color);
            return null;
        }

        private static ConstantValue SetVolume(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            TimeSpan duration = PopTimeSpan(args);
            NsRational volume = new NsRational(PopDouble(args), NssMaxVolume);

            implementation.SetVolume(entityName, duration, volume);
            return null;
        }

        private static ConstantValue Fade(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            TimeSpan duration = PopTimeSpan(args);
            var opacity = new NsRational(PopDouble(args), NssMaxOpacity);
            var easingFunction = PopEasingFunction(args);
            double delayArg = PopDouble(args, allowNull: true, allowTypeConversion: true);
            TimeSpan delay = delayArg == 1.0d ? duration : TimeSpan.FromMilliseconds(delayArg);

            implementation.Fade(entityName, duration, opacity, easingFunction, delay);
            return null;
        }

        private static ConstantValue Move(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            TimeSpan duration = PopTimeSpan(args);
            NsCoordinate x = PopCoordinate(args);
            NsCoordinate y = PopCoordinate(args);
            var easingFunction = PopEasingFunction(args);
            double delayArg = PopDouble(args, allowNull: true, allowTypeConversion: true);
            TimeSpan delay = delayArg == 1.0d ? duration : TimeSpan.FromMilliseconds(delayArg);

            implementation.Move(entityName, duration, x, y, easingFunction, delay);
            return null;
        }

        private static ConstantValue Zoom(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            TimeSpan duration = PopTimeSpan(args);
            var scaleX = new NsRational(PopDouble(args), 1000);
            var scaleY = new NsRational(PopDouble(args), 1000);
            var easingFunction = PopEasingFunction(args);
            double delayArg = PopDouble(args, allowNull: true, allowTypeConversion: true);
            TimeSpan delay = delayArg == 1.0d ? duration : TimeSpan.FromMilliseconds(delayArg);

            implementation.Zoom(entityName, duration, scaleX, scaleY, easingFunction, delay);
            return null;
        }

        private static ConstantValue CreateWindow(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            int priority = PopDouble(args);
            NsCoordinate x = PopCoordinate(args);
            NsCoordinate y = PopCoordinate(args);
            int width = PopDouble(args);
            int height = PopDouble(args);

            implementation.CreateDialogueBox(entityName, priority, x, y, width, height);
            return null;
        }

        private static ConstantValue WaitText(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            TimeSpan time = PopTimeSpan(args, allowNull: true);

            implementation.WaitText(entityName, time);
            return null;
        }

        private static ConstantValue LoadText(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string unk = PopString(args, allowNull: true);
            var boxName = PopArgument(args);
            var someStr = PopArgument(args);

            int maxWidth = PopDouble(args);
            int maxHeight = PopDouble(args);
            int letterSpacing = PopDouble(args);
            int lineSpacing = PopDouble(args);
            return null;
        }

        private static ConstantValue SetLoop(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            bool looping = PopBoolean(args);

            implementation.ToggleLooping(entityName, looping);
            return null;
        }

        private static ConstantValue SetLoopPoint(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            TimeSpan loopStart = PopTimeSpan(args);
            TimeSpan loopEnd = PopTimeSpan(args);

            implementation.SetLoopPoint(entityName, loopStart, loopEnd);
            return null;
        }

        private static ConstantValue DrawTransition(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string entityName = EntityName(PopString(args));
            TimeSpan duration = PopTimeSpan(args);
            var initialOpacity = new NsRational(PopDouble(args), NssMaxOpacity);
            var finalOpacity = new NsRational(PopDouble(args), NssMaxOpacity);
            var feather = new NsRational(PopDouble(args), 100);
            var easingFunction = PopEasingFunction(args);
            string fileName = PopString(args);
            double delayArg = PopDouble(args, allowNull: true, allowTypeConversion: true);
            TimeSpan delay = delayArg == 1.0d ? duration : TimeSpan.FromMilliseconds(delayArg);

            implementation.DrawTransition(entityName, duration, initialOpacity, finalOpacity, feather, easingFunction, fileName, delay);
            return null;
        }

        private static string EntityName(string rawEntityName)
        {
            if (string.IsNullOrEmpty(rawEntityName) || rawEntityName.Length < 2)
            {
                return rawEntityName;
            }

            return rawEntityName[0] == '@' ? rawEntityName.Substring(1) : rawEntityName;
        }

        private static ConstantValue PopArgument(Stack<ConstantValue> args)
        {
            return args.Pop();
        }

        private static string PopString(Stack<ConstantValue> args, bool allowNull = false, bool allowTypeConversion = false)
        {
            var value = PopArgument(args);
            switch (value.Type)
            {
                case BuiltInType.String:
                    return value.StringValue;

                case BuiltInType.Null:
                    return allowNull ? string.Empty : throw new InvalidOperationException();

                default:
                    return allowTypeConversion ? value.ConvertTo(BuiltInType.String).StringValue : throw new InvalidOperationException();
            }
        }

        private static int PopDouble(Stack<ConstantValue> args, bool allowNull = false, bool allowTypeConversion = false)
        {
            var value = PopArgument(args);
            switch (value.Type)
            {
                case BuiltInType.Double:
                    return (int)value.DoubleValue;

                case BuiltInType.Null:
                    return allowNull ? 0 : throw new InvalidOperationException();

                default:
                    return allowTypeConversion ? (int)value.ConvertTo(BuiltInType.Double).DoubleValue : throw new InvalidOperationException();
            }
        }

        private static bool PopBoolean(Stack<ConstantValue> args, bool allowNull = false, bool allowTypeConversion = false)
        {
            var value = PopArgument(args);
            switch (value.Type)
            {
                case BuiltInType.Boolean:
                    return value.BooleanValue;

                case BuiltInType.Null:
                    return allowNull ? false : throw new InvalidOperationException();

                default:
                    return allowTypeConversion ? value.ConvertTo(BuiltInType.Boolean).BooleanValue : throw new InvalidOperationException();
            }
        }

        private static NsCoordinate PopCoordinate(Stack<ConstantValue> args)
        {
            var value = PopArgument(args);
            switch (value.Type)
            {
                case BuiltInType.Double:
                    int i = (int)value.DoubleValue;
                    var origin = value.IsDeltaValue ? NsCoordinateOrigin.CurrentValue : NsCoordinateOrigin.Zero;
                    return new NsCoordinate(i, origin, 0.0f);

                case BuiltInType.EnumValue:
                    return NsCoordinate.FromEnumValue(value.EnumValue);

                default:
                    throw new InvalidOperationException();
            }
        }

        private static NsColor PopColor(Stack<ConstantValue> args)
        {
            var value = PopArgument(args);
            switch (value.Type)
            {
                case BuiltInType.String:
                    return NsColor.FromString(value.StringValue);

                case BuiltInType.Double:
                    return NsColor.FromRgb((int)value.DoubleValue);

                case BuiltInType.EnumValue:
                    return NsColor.FromEnumValue(value.EnumValue);

                default:
                    throw new InvalidOperationException();
            }
        }

        private static NsEasingFunction PopEasingFunction(Stack<ConstantValue> args)
        {
            return EnumConversions.ToEasingFunction(PopEnumValue(args));
        }

        private static TimeSpan PopTimeSpan(Stack<ConstantValue> args, bool allowNull = false)
        {
            int ms = PopDouble(args, allowNull);
            return TimeSpan.FromMilliseconds(ms);
        }

        private static BuiltInEnumValue PopEnumValue(Stack<ConstantValue> args, bool allowNull = true)
        {
            var value = PopArgument(args);
            switch (value.Type)
            {
                case BuiltInType.EnumValue:
                    return value.EnumValue;

                case BuiltInType.String:
                    return BuiltInEnumValue._None;

                case BuiltInType.Null:
                    return allowNull ? BuiltInEnumValue._None : throw new InvalidOperationException();

                default:
                    throw new InvalidOperationException();
            }
        }

        private static ConstantValue SetFont(EngineImplementationBase implementation, Stack<ConstantValue> args)
        {
            string fontName = PopString(args);
            int size = PopDouble(args);
            NsColor inColor = PopColor(args);
            NsColor outColor = PopColor(args);
            int fontWeight = PopDouble(args);

            string strAlignment = PopString(args);
            //TextAlignment alignment;
            //switch (strAlignment.ToUpperInvariant())
            //{
            //    case "DOWN":
            //    default:
            //        alignment = TextAlignment.Bottom;
            //        break;
            //return null; }
            return null;
        }
    }
}
