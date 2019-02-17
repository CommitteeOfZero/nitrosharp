using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NitroSharp.NsScript.Primitives;

namespace NitroSharp.NsScript.VM
{
    public class BuiltInFunctionDispatcher
    {
        private readonly ConstantValue[] _argBuffer = new ConstantValue[16];
        private readonly BuiltInFunctions _impl;
        private ConstantValue? _result;

        public BuiltInFunctionDispatcher(BuiltInFunctions functionsImpl)
        {
            _impl = functionsImpl;
        }

        private void SetResult(in ConstantValue value)
        {
            Debug.Assert(_result == null);
            _result = value;
        }

        public ConstantValue? Dispatch(BuiltInFunction function, ReadOnlySpan<ConstantValue> args)
        {
            switch (function)
            {
                case BuiltInFunction.CreateChoice:
                    CreateChoice(args);
                    break;
                case BuiltInFunction.SetAlias:
                    SetAlias(args);
                    break;
                case BuiltInFunction.Request:
                    Request(args);
                    break;
                case BuiltInFunction.Delete:
                    Delete(args);
                    break;
                case BuiltInFunction.CreateProcess:
                    CreateProcess(args);
                    break;
                case BuiltInFunction.Wait:
                    Wait(args);
                    break;
                case BuiltInFunction.WaitKey:
                    WaitKey(args);
                    break;

                case BuiltInFunction.CreateColor:
                    CreateColor(args);
                    break;
                case BuiltInFunction.LoadImage:
                    LoadImage(args);
                    break;
                case BuiltInFunction.CreateTexture:
                    CreateTexture(args);
                    break;
                case BuiltInFunction.CreateClipTexture:
                    CreateClipTexture(args);
                    break;

                case BuiltInFunction.Fade:
                    Fade(args);
                    break;
                case BuiltInFunction.Move:
                    Move(args);
                    break;
                case BuiltInFunction.Zoom:
                    Zoom(args);
                    break;

                case BuiltInFunction.CreateWindow:
                    CreateWindow(args);
                    break;
                case BuiltInFunction.LoadText:
                    LoadText(args);
                    break;
                case BuiltInFunction.WaitText:
                    WaitText(args);
                    break;

                case BuiltInFunction.CreateSound:
                    CreateSound(args);
                    break;
                case BuiltInFunction.SetVolume:
                    SetVolume(args);
                    break;
                case BuiltInFunction.SetLoop:
                    SetLoop(args);
                    break;
                case BuiltInFunction.SetLoopPoint:
                    SetLoopPoint(args);
                    break;


                case BuiltInFunction.DurationTime:
                    DurationTime(args);
                    break;
                case BuiltInFunction.PassageTime:
                    PassageTime(args);
                    break;
                case BuiltInFunction.RemainTime:
                    RemainTime(args);
                    break;
                case BuiltInFunction.ImageHorizon:
                    ImageHorizon(args);
                    break;
                case BuiltInFunction.ImageVertical:
                    ImageVertical(args);
                    break;
                case BuiltInFunction.Random:
                    Random(args);
                    break;
                case BuiltInFunction.SoundAmplitude:
                    SoundAmplitude(args);
                    break;
                case BuiltInFunction.Platform:
                    Platform(args);
                    break;
                case BuiltInFunction.ModuleFileName:
                    ModuleFileName(args);
                    break;
                case BuiltInFunction.String:
                    String(args);
                    break;
                case BuiltInFunction.Time:
                    Time(args);
                    break;
            }

            ConstantValue? result = _result;
            _result = null;
            return result;
        }

        private void CreateWindow(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, 6);
            string entityName = EntityName(args, 0);
            int priority = AssertInteger(args, 1);
            NsCoordinate x = AssertCoordinate(args, 2);
            NsCoordinate y = AssertCoordinate(args, 3);
            int width = AssertInteger(args, 4);
            int height = AssertInteger(args, 5);
            _impl.CreateDialogueBox(entityName, priority, x, y, width, height);
        }

        private void WaitText(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, 2);
            _impl.WaitText(string.Empty, default);
        }

        private void LoadText(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 7);
            string subroutineName = AssertString(args, 0);
            string boxName = AssertString(args, 1);
            string textName = AssertString(args, 2);
            int maxWidth = AssertInteger(args, 3);
            int maxHeight = AssertInteger(args, 4);
            int letterSpacing = AssertInteger(args, 5);
            int lineSpacing = AssertInteger(args, 6);

            NsxModule module = _impl.Interpreter.CurrentThread!.CallFrameStack.Peek(1).Module;
            int subroutineIdx = module.LookupSubroutineIndex(subroutineName);
            ref readonly SubroutineRuntimeInformation srti = ref module.GetSubroutineRuntimeInformation(subroutineIdx);
            int blockIndex = srti.LookupDialogueBlockIndex(textName);
            int offset = module.GetSubroutine(subroutineIdx).DialogueBlockOffsets[blockIndex];

            var token = new DialogueBlockToken(textName, boxName, module, subroutineIdx, offset);
            _impl.LoadText(token, maxWidth, maxHeight, letterSpacing, lineSpacing);
        }

        private void SetVolume(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 3);
            string entityName = EntityName(args, 0);
            TimeSpan duration = Time(AssertInteger(args, 1));
            var volume = new NsRational(AssertInteger(args, 2), 1000);
            _impl.SetVolume(entityName, duration, volume);
        }

        private string EntityName(ReadOnlySpan<ConstantValue> args, int index)
        {
            string name = AssertString(args, index);
            return name.StartsWith("@") ? name.Substring(1) : name;
        }

        private void CreateSound(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 3);
            string entityName = EntityName(args, 0);
            NsAudioKind kind = EnumConversions.ToAudioKind(AssertBuiltInConstant(args, 1));
            string fileName = AssertString(args, 2);
            _impl.LoadAudio(entityName, kind, fileName);
        }

        private void WaitKey(ReadOnlySpan<ConstantValue> args)
        {
            if (args.Length > 0)
            {
                TimeSpan timeout = Time(AssertInteger(args, 0));
                _impl.WaitForInput(timeout);
            }
            else
            {
                _impl.WaitForInput();
            }
        }

        private void Wait(ReadOnlySpan<ConstantValue> args)
        {
            TimeSpan delay = Time(AssertInteger(args, 0));
            _impl.Delay(delay);
        }

        private void SetLoopPoint(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 3);
            string entityName = EntityName(args, 0);
            TimeSpan loopStart = Time(AssertInteger(args, 1));
            TimeSpan loopEnd = Time(AssertInteger(args, 2));
            _impl.SetLoopRegion(entityName, loopStart, loopEnd);
        }

        private void SetLoop(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 2);
            string entityName = EntityName(args, 0);
            bool looping = AssertBool(args, 1);
            _impl.ToggleLooping(entityName, looping);
        }

        private void Zoom(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 6);
            string entityName = EntityName(args, 0);
            TimeSpan duration = Time(AssertInteger(args, 1));
            var dstScaleX = new NsRational(AssertInteger(args, 2), 1000);
            var dstScaleY = new NsRational(AssertInteger(args, 3), 1000);
            NsEasingFunction easingFn = AssertEasingFunction(args, 4);
            int delayArg = AssertInteger(args, 5);
            TimeSpan delay = delayArg == 1 ? duration : Time(delayArg);
            _impl.Zoom(entityName, duration, dstScaleX, dstScaleY, easingFn, delay);
        }

        private void Move(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 6);
            string entityName = EntityName(args, 0);
            TimeSpan duration = Time(AssertInteger(args, 1));
            NsCoordinate dstX = AssertCoordinate(args, 2);
            NsCoordinate dstY = AssertCoordinate(args, 3);
            NsEasingFunction easingFn = AssertEasingFunction(args, 4);
            int delayArg = AssertInteger(args, 5);
            TimeSpan delay = delayArg == 1 ? duration : Time(delayArg);
            _impl.Move(entityName, duration, dstX, dstY, easingFn, delay);
        }

        private TimeSpan Time(int ms) => TimeSpan.FromMilliseconds(ms);

        private NsCoordinate AssertCoordinate(ReadOnlySpan<ConstantValue> args, int index)
        {
            ref readonly ConstantValue val = ref args[index];
            return val.Type switch
            {
                BuiltInType.Integer
                    => new NsCoordinate(val.AsInteger()!.Value, NsCoordinateOrigin.Zero, 0),
                BuiltInType.DeltaInteger
                    => new NsCoordinate(val.AsDelta()!.Value, NsCoordinateOrigin.CurrentValue, 0),
                BuiltInType.BuiltInConstant
                    => NsCoordinate.FromEnumValue(val.AsBuiltInConstant()!.Value),

                _ => UnexpectedArgType<NsCoordinate>(index, BuiltInType.Integer, val.Type)
            };
        }

        private NsEasingFunction AssertEasingFunction(ReadOnlySpan<ConstantValue> args, int index)
        {
            ref readonly ConstantValue val = ref args[index];
            if (val.Type == BuiltInType.BuiltInConstant)
            {
                return EnumConversions.ToEasingFunction(val.AsBuiltInConstant()!.Value);
            }
            else if (val.Type == BuiltInType.Null)
            {
                return NsEasingFunction.None;
            }

            return UnexpectedArgType<NsEasingFunction>(
                index, BuiltInType.BuiltInConstant, val.Type);
        }

        private void Fade(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 5);
            string entityName = EntityName(args, 0);
            TimeSpan duration = Time(AssertInteger(args, 1));
            var dstOpacity = new NsRational(AssertInteger(args, 2), 1000);
            NsEasingFunction easingFn = AssertEasingFunction(args, 3);
            int delayArg = AssertInteger(args, 4);
            TimeSpan delay = delayArg == 1 ? duration : Time(delayArg);
            _impl.Fade(entityName, duration, dstOpacity, easingFn, delay);
        }

        private void SetAlias(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 2);
            string entityName = EntityName(args, 0);
            string alias = AssertString(args, 1);
            _impl.SetAlias(entityName, alias);
        }

        private void Request(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 2);
            string entityName = EntityName(args, 0);
            BuiltInConstant constant = AssertBuiltInConstant(args, 1);
            NsEntityAction action = EnumConversions.ToEntityAction(constant);
            _impl.Request(entityName, action);
        }

        private void Delete(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 1);
            string entityName = EntityName(args, 0);
            _impl.RemoveEntity(entityName);
        }

        private void CreateProcess(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 5);
            string name = AssertString(args, 0);
            string target = AssertString(args, 4);
            _impl.CreateThread(name, target);
        }

        private void CreateChoice(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 1);
            string entityName = EntityName(args, 0);
            _impl.CreateChoice(entityName);
        }

        private void CreateColor(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 7);
            string entityName = EntityName(args, 0);
            int priority = AssertInteger(args, 1);
            NsCoordinate x = AssertCoordinate(args, 2);
            NsCoordinate y = AssertCoordinate(args, 3);
            int width = AssertInteger(args, 4);
            int height = AssertInteger(args, 5);
            NsColor color = AssertColor(args, 6);
            _impl.FillRectangle(entityName, priority, x, y, width, height, color);
        }

        private NsColor AssertColor(ReadOnlySpan<ConstantValue> args, int index)
        {
            ref readonly ConstantValue val = ref args[index];
            return val.Type switch
            {
                BuiltInType.String => NsColor.FromString(val.AsString()!),
                BuiltInType.Integer => NsColor.FromRgb(val.AsInteger()!.Value),
                BuiltInType.BuiltInConstant => NsColor.FromConstant(val.AsBuiltInConstant()!.Value),
                _ => UnexpectedArgType<NsColor>(index, BuiltInType.Null, val.Type)
            };
        }

        private void LoadImage(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 2);
            string entityName = EntityName(args, 0);
            string fileName = AssertString(args, 1);
            _impl.LoadImage(entityName, fileName);
        }

        private void CreateTexture(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 5);
            string entityName = EntityName(args, 0);
            int priority = AssertInteger(args, 1);
            NsCoordinate x = AssertCoordinate(args, 2);
            NsCoordinate y = AssertCoordinate(args, 3);

            ref readonly ConstantValue arg4 = ref args[4];
            string fileOrEntityName = arg4.Type switch
            {
                BuiltInType.String => EntityName(args, 4),
                BuiltInType.BuiltInConstant => arg4.ConvertToString(),
                _ => UnexpectedArgType<string>(4, BuiltInType.String, arg4.Type)
            };

            _impl.CreateSprite(entityName, priority, x, y, fileOrEntityName);
        }

        private void CreateClipTexture(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 9);
            string entityName = EntityName(args, 0);
            int priority = AssertInteger(args, 1);
            NsCoordinate x1 = AssertCoordinate(args, 2);
            NsCoordinate y1 = AssertCoordinate(args, 3);
            NsCoordinate x2 = AssertCoordinate(args, 4);
            NsCoordinate y2 = AssertCoordinate(args, 5);
            int width = AssertInteger(args, 6);
            int height = AssertInteger(args, 7);
            string srcEntityName = AssertString(args, 8);
            _impl.CreateSpriteEx(entityName, priority, x1, y1, x2, y2, width, height, srcEntityName);
        }

        private void Time(ReadOnlySpan<ConstantValue> args)
        {
            SetResult(ConstantValue.Integer(0));
        }

        private void String(ReadOnlySpan<ConstantValue> args)
        {
            string format = AssertString(args, 0);
            var list = new List<object>();
            foreach (ref readonly ConstantValue arg in args.Slice(1))
            {
                int? num = arg.AsInteger();
                if (num != null)
                {
                    list.Add(num.Value);
                }
                else
                {
                    list.Add(arg.ConvertToString());
                }
            }

            ConstantValue f = _impl.FormatString(format, list.ToArray());
            Console.WriteLine(f.AsString()!);
            Console.WriteLine(list[0].ToString());
            SetResult(f);
        }

        private void ModuleFileName(ReadOnlySpan<ConstantValue> args)
        {
            SetResult(ConstantValue.String(_impl.GetCurrentModuleName()));
        }

        private void Platform(ReadOnlySpan<ConstantValue> args)
        {
            SetResult(ConstantValue.Integer(_impl.GetPlatformId()));
        }

        private void SoundAmplitude(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 1);
            string characterName = AssertString(args, 0);
            SetResult(ConstantValue.Integer(_impl.GetSoundAmplitude(characterName)));
        }

        private void Random(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 1);
            int max = AssertInteger(args, 0);
            SetResult(ConstantValue.Integer(_impl.GenerateRandomNumber(max)));
        }

        private void ImageHorizon(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 1);
            string entityName = EntityName(args, 0);
            SetResult(ConstantValue.Integer(_impl.GetWidth(entityName)));
        }

        private void ImageVertical(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 1);
            string entityName = EntityName(args, 0);
            SetResult(ConstantValue.Integer(_impl.GetHeight(entityName)));
        }

        private void RemainTime(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 1);
            string entityName = EntityName(args, 0);
            SetResult(ConstantValue.Integer(_impl.GetTimeRemaining(entityName)));
        }

        private void PassageTime(ReadOnlySpan<ConstantValue> args)
        {
            int a = -+-+-+1;
            args = AssertArgs(args, countRequired: 1);
            string entityName = EntityName(args, 0);
            SetResult(ConstantValue.Integer(_impl.GetTimeElapsed(entityName)));
        }

        private void DurationTime(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 1);
            string entityName = EntityName(args, 0);
            SetResult(ConstantValue.Integer(_impl.GetSoundDuration(entityName)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string AssertString(ReadOnlySpan<ConstantValue> args, int index)
        {
            ref readonly ConstantValue arg = ref args[index];
            return arg.AsString()
                ?? UnexpectedArgType<string>(index, BuiltInType.String, arg.Type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AssertInteger(ReadOnlySpan<ConstantValue> args, int index)
        {
            ref readonly ConstantValue arg = ref args[index];
            return arg.AsInteger()
                ?? UnexpectedArgType<int>(index, BuiltInType.Integer, arg.Type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AssertBool(ReadOnlySpan<ConstantValue> args, int index)
        {
            ref readonly ConstantValue arg = ref args[index];
            return arg.AsBool()
                ?? UnexpectedArgType<bool>(index, BuiltInType.Boolean, arg.Type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BuiltInConstant AssertBuiltInConstant(ReadOnlySpan<ConstantValue> args, int index)
        {
            ref readonly ConstantValue arg = ref args[index];
            return arg.AsBuiltInConstant()
                ?? UnexpectedArgType<BuiltInConstant>(
                    index, BuiltInType.BuiltInConstant, arg.Type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<ConstantValue> AssertArgs(
            ReadOnlySpan<ConstantValue> providedArgs, int countRequired)
        {
            return providedArgs.Length == countRequired
                ? providedArgs
                : handleRareCase(providedArgs, countRequired);

            ReadOnlySpan<ConstantValue> handleRareCase(
                ReadOnlySpan<ConstantValue> providedArgs, int countRequired)
            {
                if (providedArgs.Length > countRequired)
                {
                    return providedArgs.Slice(0, countRequired);
                }
                else
                {
                    providedArgs.CopyTo(_argBuffer);
                    for (int i = providedArgs.Length; i < countRequired; i++)
                    {
                        _argBuffer[i] = ConstantValue.Integer(0);
                    }

                    return _argBuffer.AsSpan(0, countRequired);
                }
            }
        }

        private void Fail()
        {
            throw new NotImplementedException();
        }

        private void AssertEq(ReadOnlySpan<ConstantValue> args)
        {
            throw new NotImplementedException();
        }

        private void Assert(ReadOnlySpan<ConstantValue> args)
        {
        }

        private void Log(ReadOnlySpan<ConstantValue> args)
        {
            throw new NotImplementedException();
        }

        private T UnexpectedArgType<T>(int index, BuiltInType expectedType, BuiltInType actualType)
            => throw new NsxCallDispatchException(index, expectedType, actualType);
    }
}
