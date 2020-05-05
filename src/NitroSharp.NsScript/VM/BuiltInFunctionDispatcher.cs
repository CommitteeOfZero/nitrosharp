using System;
using System.Collections.Generic;
using System.Diagnostics;
using NitroSharp.NsScript.Primitives;

namespace NitroSharp.NsScript.VM
{
    internal sealed class BuiltInFunctionDispatcher
    {
        private BuiltInFunctions _impl;
        private ConstantValue? _result;

        public BuiltInFunctionDispatcher()
        {
            _impl = null!;
        }

        private void SetResult(in ConstantValue value)
        {
            Debug.Assert(_result == null);
            _result = value;
        }

        public ConstantValue? Dispatch(
            BuiltInFunctions impl,
            BuiltInFunction function,
            ReadOnlySpan<ConstantValue> cvs)
        {
            _impl = impl;
            var args = new ArgConsumer(cvs);
            switch (function)
            {
                case BuiltInFunction.CreateName:
                    CreateName(ref args);
                    break;
                case BuiltInFunction.SetAlias:
                    SetAlias(ref args);
                    break;
                case BuiltInFunction.Request:
                    Request(ref args);
                    break;
                case BuiltInFunction.Delete:
                    Delete(ref args);
                    break;
                case BuiltInFunction.CreateProcess:
                    CreateProcess(ref args);
                    break;
                case BuiltInFunction.Wait:
                    Wait(ref args);
                    break;
                case BuiltInFunction.WaitKey:
                    WaitKey(ref args);
                    break;

                case BuiltInFunction.CreateChoice:
                    CreateChoice(ref args);
                    break;
                case BuiltInFunction.CreateColor:
                    CreateColor(ref args);
                    break;
                case BuiltInFunction.LoadImage:
                    LoadImage(ref args);
                    break;
                case BuiltInFunction.CreateTexture:
                    CreateTexture(ref args);
                    break;
                case BuiltInFunction.CreateClipTexture:
                    CreateClipTexture(ref args);
                    break;
                case BuiltInFunction.DrawTransition:
                    DrawTransition(ref args);
                    break;
                case BuiltInFunction.CreateMask:
                    CreateMask(ref args);
                    break;
                case BuiltInFunction.CreateWindow:
                    CreateWindow(ref args);
                    break;
                case BuiltInFunction.SetShade:
                    SetShade(ref args);
                    break;
                case BuiltInFunction.SetTone:
                    SetTone(ref args);
                    break;

                case BuiltInFunction.CreateEffect:
                    CreateEffect(ref args);
                    break;

                case BuiltInFunction.Fade:
                    Fade(ref args);
                    break;
                case BuiltInFunction.Move:
                    Move(ref args);
                    break;
                case BuiltInFunction.Zoom:
                    Zoom(ref args);
                    break;
                case BuiltInFunction.Rotate:
                    Rotate(ref args);
                    break;
                case BuiltInFunction.BezierMove:
                    BezierMove(ref args);
                    break;
                case BuiltInFunction.WaitAction:
                    WaitAction(ref args);
                    break;
                case BuiltInFunction.WaitMove:
                    WaitMove(ref args);
                    break;
                case BuiltInFunction.CreateText:
                    CreateText(ref args);
                    break;
                case BuiltInFunction.LoadText:
                    LoadText(ref args);
                    break;
                case BuiltInFunction.WaitText:
                    WaitText();
                    break;

                case BuiltInFunction.CreateSound:
                    CreateSound(ref args);
                    break;
                case BuiltInFunction.SetVolume:
                    SetVolume(ref args);
                    break;
                case BuiltInFunction.SetLoop:
                    SetLoop(ref args);
                    break;
                case BuiltInFunction.SetLoopPoint:
                    SetLoopPoint(ref args);
                    break;

                case BuiltInFunction.DurationTime:
                    DurationTime(ref args);
                    break;
                case BuiltInFunction.PassageTime:
                    PassageTime(ref args);
                    break;
                case BuiltInFunction.RemainTime:
                    RemainTime(ref args);
                    break;
                case BuiltInFunction.ImageHorizon:
                    ImageHorizon(ref args);
                    break;
                case BuiltInFunction.ImageVertical:
                    ImageVertical(ref args);
                    break;
                case BuiltInFunction.Random:
                    Random(ref args);
                    break;
                case BuiltInFunction.SoundAmplitude:
                    SoundAmplitude(ref args);
                    break;
                case BuiltInFunction.Platform:
                    Platform();
                    break;
                case BuiltInFunction.ModuleFileName:
                    ModuleFileName();
                    break;
                case BuiltInFunction.String:
                    String(ref args);
                    break;
                case BuiltInFunction.Integer:
                    Integer(ref args);
                    break;
                case BuiltInFunction.Time:
                    Time();
                    break;
                case BuiltInFunction.ScrollbarValue:
                    ScrollbarValue(ref args);
                    break;

                case BuiltInFunction.assert:
                    Assert(ref args);
                    break;
                case BuiltInFunction.assert_eq:
                    AssertEqual(ref args);
                    break;

                case BuiltInFunction.XBOX360_LockVideo:
                    XBOX360_LockVideo(ref args);
                    break;
                case BuiltInFunction.XBOX360_IsSignin:
                    XBOX360_IsSignin(ref args);
                    break;
                case BuiltInFunction.XBOX360_Presence:
                    XBOX360_Presence(ref args);
                    break;
                case BuiltInFunction.XBOX360_Achieved:
                    XBOX360_Achieved(ref args);
                    break;
                case BuiltInFunction.XBOX360_CheckStorage:
                    XBOX360_CheckStorage(ref args);
                    break;
                case BuiltInFunction.XBOX360_UserIndex:
                    XBOX360_UserIndex(ref args);
                    break;
                case BuiltInFunction.XBOX360_CurrentStorage:
                    XBOX360_CurrentStorage(ref args);
                    break;
                case BuiltInFunction.XBOX360_StorageSize:
                    XBOX360_StorageSize(ref args);
                    break;
                case BuiltInFunction.XBOX360_ExistContent:
                    XBOX360_ExistContent(ref args);
                    break;
                case BuiltInFunction.XBOX360_PadTrigger:
                    XBOX360_PadTrigger(ref args);
                    break;
                case BuiltInFunction.XBOX360_SelectStorage:
                    XBOX360_SelectStorage(ref args);
                    break;
                case BuiltInFunction.XBOX360_InitUser:
                    XBOX360_InitUser();
                    break;
                case BuiltInFunction.XBOX360_AwardGameIcon:
                    XBOX360_AwardGameIcon();
                    break;
            }

            ConstantValue? result = _result;
            _result = null;
            return result;
        }

        private void WaitMove(ref ArgConsumer args)
        {
            _impl.WaitMove(args.TakeEntityQuery());
        }

        private void WaitAction(ref ArgConsumer args)
        {
            _impl.WaitAction(args.TakeEntityQuery(), args.TakeTimeSpan());
        }

        private void CreateName(ref ArgConsumer args)
        {
            _impl.CreateEntity(args.TakeEntityPath());
        }

        private void BezierMove(ref ArgConsumer args)
        {
            _impl.BezierMove(
                args.TakeEntityQuery(),
                duration: args.TakeTimeSpan(),
                args.TakeBezierCurve(),
                args.TakeEaseFunction(),
                wait: args.TakeBool()
            );
        }

        private void CreateMask(ref ArgConsumer args)
        {
            _impl.CreateAlphaMask(
                args.TakeEntityPath(),
                priority: args.TakeInt(),
                x: args.TakeCoordinate(),
                y: args.TakeCoordinate(),
                maskPath: args.TakeString(),
                inheritTransform: args.TakeBool()
            );
        }

        private void CreateEffect(ref ArgConsumer args)
        {
            _impl.CreateEffect(
                args.TakeEntityPath(),
                priority: args.TakeInt(),
                x: args.TakeCoordinate(),
                y: args.TakeCoordinate(),
                width: args.TakeUInt(),
                height: args.TakeUInt(),
                effectName: args.TakeString()
            );
        }

        private void SetTone(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            if (args.TakeConstant() != BuiltInConstant.Monochrome)
            {
                throw new Exception();
            }

            _impl.Grayscale(query);
        }

        private void SetShade(ref ArgConsumer args)
        {
            _impl.BoxBlur(
                args.TakeEntityQuery(),
                nbPasses: args.TakeConstant() switch
                {
                    BuiltInConstant.Heavy => 16u,
                    BuiltInConstant.Medium => 8u,
                    _ => throw new Exception()
                }
            );
        }

        private void CreateText(ref ArgConsumer args)
        {
            _impl.CreateTextBlock(
                args.TakeEntityPath(),
               priority: args.TakeInt(),
               x: args.TakeCoordinate(),
               y: args.TakeCoordinate(),
               width: args.TakeDimension(),
               height: args.TakeDimension(),
               pxmlText: args.TakeString()
            );
        }

        private void ScrollbarValue(ref ArgConsumer args)
        {
            EntityPath scrollbarEntity = args.TakeEntityPath();
            SetResult(ConstantValue.Float(_impl.GetScrollbarValue(scrollbarEntity)));
        }

        private void Integer(ref ArgConsumer args)
        {
            ConstantValue value = args.TakeOpt(ConstantValue.Null);
            switch (value.Type)
            {
                case BuiltInType.Integer:
                    break;
                case BuiltInType.Float:
                    value = ConstantValue.Integer((int)value.AsFloat()!.Value);
                    break;
                    //default:
                    //    UnexpectedArgType<ConstantValue>(0, BuiltInType.Integer, value.Type);
                    //    break;
            }

            SetResult(value);
        }

        private void CreateWindow(ref ArgConsumer args)
        {
            _impl.CreateDialogueBox(
                args.TakeEntityPath(),
                priority: args.TakeInt(),
                x: args.TakeCoordinate(),
                y: args.TakeCoordinate(),
                width: args.TakeUInt(),
                height: args.TakeUInt(),
                inheritTransform: args.TakeBool()
            );
        }

        private void WaitText()
        {
            _impl.WaitText(string.Empty, default);
        }

        private void LoadText(ref ArgConsumer args)
        {
            string subroutineName = args.TakeString();
            string boxName = args.TakeString();
            string textName = args.TakeString();
            uint maxWidth = args.TakeUInt();
            uint maxHeight = args.TakeUInt();
            int letterSpacing = args.TakeInt();
            int lineSpacing = args.TakeInt();

            NsxModule module = _impl.VM.CurrentThread!.CallFrameStack.Peek(1).Module;
            int subroutineIdx = module.LookupSubroutineIndex(subroutineName);
            ref readonly SubroutineRuntimeInfo srti = ref module.GetSubroutineRuntimeInfo(subroutineIdx);
            int blockIndex = srti.LookupDialogueBlockIndex(textName);
            int offset = module.GetSubroutine(subroutineIdx).DialogueBlockOffsets[blockIndex];

            var token = new DialogueBlockToken(textName, boxName, module, subroutineIdx, offset);
            _impl.LoadText(token, maxWidth, maxHeight, letterSpacing, lineSpacing);
        }

        private void SetVolume(ref ArgConsumer args)
        {
            _impl.SetVolume(
                args.TakeEntityPath(),
                duration: args.TakeTimeSpan(),
                volume: args.TakeRational()
            );
        }

        private void CreateSound(ref ArgConsumer args)
        {
            _impl.LoadAudio(
                args.TakeEntityPath(),
                kind: args.TakeAudioKind(),
                fileName: args.TakeString()
            );
        }

        private void WaitKey(ref ArgConsumer args)
        {
            if (args.Count > 0)
            {
                TimeSpan timeout = args.TakeTimeSpan();
                _impl.WaitForInput(timeout);
            }
            else
            {
                _impl.WaitForInput();
            }
        }

        private void Wait(ref ArgConsumer args)
        {
            _impl.Delay(args.TakeTimeSpan());
        }

        private void SetLoopPoint(ref ArgConsumer args)
        {
            _impl.SetLoopRegion(
                args.TakeEntityPath(),
                loopStart: args.TakeTimeSpan(),
                loopEnd: args.TakeTimeSpan()
            );
        }

        private void SetLoop(ref ArgConsumer args)
        {
            _impl.ToggleLooping(
                args.TakeEntityPath(),
                looping: args.TakeBool()
            );
        }

        private void Zoom(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            TimeSpan duration = args.TakeTimeSpan();
            _impl.Zoom(
                query,
                duration,
                dstScaleX: args.TakeRational(),
                dstScaleY: args.TakeRational(),
                easeFunction: args.TakeEaseFunction(),
                args.TakeAnimDelay(duration)
            );
        }

        private void Rotate(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            TimeSpan duration = args.TakeTimeSpan();
            _impl.Rotate(
                query,
                duration,
                dstRotationX: args.TakeNumeric(),
                dstRotationY: args.TakeNumeric(),
                dstRotationZ: args.TakeNumeric(),
                args.TakeEaseFunction(),
                args.TakeAnimDelay(duration)
            );
        }

        private void Move(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            TimeSpan duration = args.TakeTimeSpan();
            _impl.Move(
                query,
                duration,
                dstX: args.TakeCoordinate(),
                dstY: args.TakeCoordinate(),
                easeFunction: args.TakeEaseFunction(),
                delay: args.TakeAnimDelay(duration)
            );
        }

        private void Fade(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            TimeSpan duration = args.TakeTimeSpan();
            _impl.Fade(
                query,
                duration,
                dstOpacity: args.TakeRational(),
                easeFunction: args.TakeEaseFunction(),
                args.TakeAnimDelay(duration)
            );
        }

        private void DrawTransition(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            TimeSpan duration = args.TakeTimeSpan();
            _impl.DrawTransition(
                query,
                duration,
                initialFadeAmount: args.TakeRational(),
                finalFadeAmount: args.TakeRational(),
                feather: args.TakeRational(),
                args.TakeEaseFunction(),
                maskFileName: args.TakeString(),
                delay: args.TakeAnimDelay(duration)
            );
        }

        private void SetAlias(ref ArgConsumer args)
        {
            _impl.SetAlias(
                args.TakeEntityPath(),
                alias: args.TakeEntityPath()
            );
        }

        private void Request(ref ArgConsumer args)
        {
            _impl.Request(args.TakeEntityQuery(), args.TakeEntityAction());
        }

        private void Delete(ref ArgConsumer args)
        {
            _impl.DestroyEntities(args.TakeEntityQuery());
        }

        private void CreateProcess(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            args.Skip();
            args.Skip();
            args.Skip();
            string target = args.TakeString();
            _impl.CreateThread(entityPath, target);
        }

        private void CreateChoice(ref ArgConsumer args)
        {
            _impl.CreateChoice(args.TakeEntityPath());
        }

        private void CreateColor(ref ArgConsumer args)
        {
            static NsColor takeColor(ref ArgConsumer args)
            {
                // Special case:
                // CreateColor("back05", 100, 0, 0, 1280, 720, null, "Black");
                //                                             ^^^^
                if (args.Count == 8)
                {
                    args.Skip();
                }

                return args.TakeColor();
            }

            _impl.CreateRectangle(
                args.TakeEntityPath(),
                priority: args.TakeInt(),
                x: args.TakeCoordinate(),
                y: args.TakeCoordinate(),
                width: args.TakeUInt(),
                height: args.TakeUInt(),
                color: takeColor(ref args)
            );
        }

        private void LoadImage(ref ArgConsumer args)
        {
            _impl.LoadImage(
                args.TakeEntityPath(),
                fileName: args.TakeString()
            );
        }

        private void CreateTexture(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            int priority = args.TakeInt();
            NsCoordinate x = args.TakeCoordinate();
            NsCoordinate y = args.TakeCoordinate();

            ConstantValue arg4 = args.TakeOpt(ConstantValue.Null);
            string fileOrEntityName = arg4.Type switch
            {
                BuiltInType.String => arg4.AsString()!,
                BuiltInType.BuiltInConstant => arg4.ConvertToString(),
                _ => args.UnexpectedType<string>(arg4.Type)
            };

            _impl.CreateSprite(entityPath, priority, x, y, fileOrEntityName);
        }

        private void CreateClipTexture(ref ArgConsumer args)
        {
            _impl.CreateSpriteEx(
                args.TakeEntityPath(),
                priority: args.TakeInt(),
                x: args.TakeCoordinate(),
                y: args.TakeCoordinate(),
                srcX: args.TakeUInt(),
                srcY: args.TakeUInt(),
                width: args.TakeUInt(),
                height: args.TakeUInt(),
                args.TakeEntityPath()
            );
        }

        private void Time()
        {
            SetResult(ConstantValue.Integer(0));
        }

        private void String(ref ArgConsumer args)
        {
            string format = args.TakeString();
            var list = new List<object>();
            foreach (ref readonly ConstantValue arg in args.AsSpan(1))
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

            SetResult(_impl.FormatString(format, list.ToArray()));
        }

        private void ModuleFileName()
        {
            SetResult(ConstantValue.String(_impl.GetCurrentModuleName()));
        }

        private void Platform()
        {
            SetResult(ConstantValue.Integer(_impl.GetPlatformId()));
        }

        private void SoundAmplitude(ref ArgConsumer args)
        {
            string characterName = args.TakeString();
            SetResult(ConstantValue.Integer(_impl.GetSoundAmplitude(characterName)));
        }

        private void Random(ref ArgConsumer args)
        {
            int max = args.TakeInt();
            SetResult(ConstantValue.Integer(_impl.GenerateRandomNumber(max)));
        }

        private void ImageHorizon(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            SetResult(ConstantValue.Integer(_impl.GetWidth(entityPath)));
        }

        private void ImageVertical(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            SetResult(ConstantValue.Integer(_impl.GetHeight(entityPath)));
        }

        private void RemainTime(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            SetResult(ConstantValue.Integer(_impl.GetTimeRemaining(entityPath)));
        }

        private void PassageTime(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            SetResult(ConstantValue.Integer(_impl.GetTimeElapsed(entityPath)));
        }

        private void DurationTime(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            SetResult(ConstantValue.Integer(_impl.GetSoundDuration(entityPath)));
        }

        private void XBOX360_AwardGameIcon()
        {
        }

        private void XBOX360_InitUser()
        {
        }

        private void XBOX360_SelectStorage(ref ArgConsumer args)
        {
            int storage = args.TakeInt();
            _result = ConstantValue.True;
        }

        private void XBOX360_PadTrigger(ref ArgConsumer args)
        {
            int unk = args.TakeInt();
            _result = ConstantValue.Integer(0);
        }

        private void XBOX360_ExistContent(ref ArgConsumer args)
        {
            _result = ConstantValue.True;
        }

        private void XBOX360_StorageSize(ref ArgConsumer args)
        {
            _result = ConstantValue.Integer(int.MaxValue);
        }

        private void XBOX360_CurrentStorage(ref ArgConsumer args)
        {
            _result = ConstantValue.Integer(0);
        }

        private void XBOX360_UserIndex(ref ArgConsumer args)
        {
            _result = ConstantValue.Integer(0);
        }

        private void XBOX360_CheckStorage(ref ArgConsumer args)
        {
            _result = ConstantValue.True;
        }

        private void XBOX360_Achieved(ref ArgConsumer args)
        {
            int unk = args.TakeInt();
        }

        private void XBOX360_Presence(ref ArgConsumer args)
        {
            int unk = args.TakeInt();
        }

        private void XBOX360_IsSignin(ref ArgConsumer args)
        {
            _result = ConstantValue.True;
        }

        private void XBOX360_LockVideo(ref ArgConsumer args)
        {
            bool unk = args.TakeBool();
        }

        private void Fail()
        {
            throw new NotImplementedException();
        }

        private void AssertEqual(ref ArgConsumer args)
        {
            _impl.AssertTrue(args.Take().Equals(args.Take()));
        }

        private void Assert(ref ArgConsumer args)
        {
            _impl.AssertTrue(args.TakeBool());
        }

        private void Log(ref ArgConsumer args)
        {
        }

        private ref struct ArgConsumer
        {
            private readonly ReadOnlySpan<ConstantValue> _args;
            private int _pos;

            public ArgConsumer(ReadOnlySpan<ConstantValue> args)
            {
                _args = args;
                _pos = 0;
            }

            private static TimeSpan Time(int ms) => TimeSpan.FromMilliseconds(ms);

            public int Count => _args.Length;

            public ReadOnlySpan<ConstantValue> AsSpan(int start) => _args.Slice(start);
            public void Skip() => _pos++;

            public ConstantValue Take()
            {
                return _pos < _args.Length
                    ? _args[_pos++]
                    : throw new InvalidOperationException("Argument not provided.");
            }

            public ConstantValue TakeOpt(in ConstantValue defaultValue)
            {
                return _pos < _args.Length
                    ? _args[_pos++]
                    : defaultValue;
            }

            public EntityPath TakeEntityPath() => new EntityPath(TakeString());
            public EntityQuery TakeEntityQuery() => new EntityQuery(TakeString());

            public string TakeString()
            {
                ConstantValue arg = TakeOpt(ConstantValue.EmptyString);
                return arg.IsString ? arg.AsString()! : arg.ConvertToString();
            }

            public uint TakeUInt()
            {
                int value = TakeInt();
                Debug.Assert(value >= 0);
                return (uint)value;
            }

            public int TakeInt()
            {
                ConstantValue arg = TakeOpt(ConstantValue.Integer(0));
                return arg.AsInteger()!.Value;
            }

            public TimeSpan TakeTimeSpan()
            {
                ConstantValue val = TakeOpt(ConstantValue.Integer(0));
                int num = val.Type switch
                {
                    BuiltInType.Integer => val.AsInteger()!.Value,
                    BuiltInType.String => int.Parse(val.AsString()!),
                    BuiltInType.Null => 0,
                    _ => UnexpectedType<int>(val.Type)
                };
                return Time(num);
            }

            public bool TakeBool()
            {
                ConstantValue arg = TakeOpt(ConstantValue.False);
                return arg.AsBool()!.Value;
            }

            public NsColor TakeColor()
            {
                ConstantValue val = TakeOpt(ConstantValue.Null);
                return val.Type switch
                {
                    BuiltInType.String => NsColor.FromString(val.AsString()!),
                    BuiltInType.Integer => NsColor.FromRgb(val.AsInteger()!.Value),
                    BuiltInType.BuiltInConstant => NsColor.FromConstant(val.AsBuiltInConstant()!.Value),
                    _ => UnexpectedType<NsColor>(val.Type)
                };
            }

            public NsCoordinate TakeCoordinate()
            {
                ConstantValue val = TakeOpt(ConstantValue.Integer(0));
                NsCoordinate? ret = NsCoordinate.FromValue(val);
                return ret ?? UnexpectedType<NsCoordinate>(val.Type);
            }

            public NsTextDimension TakeDimension()
            {
                ConstantValue val = TakeOpt(ConstantValue.Integer(0));
                return val.Type switch
                {
                    BuiltInType.Integer
                        => NsTextDimension.WithValue(val.AsInteger()!.Value),
                    BuiltInType.BuiltInConstant
                        => NsTextDimension.FromConstant(val.AsBuiltInConstant()!.Value),
                    _ => UnexpectedType<NsTextDimension>(val.Type)
                };
            }

            public NsEaseFunction TakeEaseFunction()
            {
                ConstantValue val = TakeOpt(ConstantValue.Null);
                return val.Type switch
                {
                    BuiltInType.BuiltInConstant
                        => EnumConversions.ToEaseFunction(val.AsBuiltInConstant()!.Value),
                    BuiltInType.Null => NsEaseFunction.None,
                    _ => UnexpectedType<NsEaseFunction>(val.Type)
                };
            }

            public NsAudioKind TakeAudioKind()
            {
                ConstantValue val = TakeOpt(ConstantValue.Null);
                return val.Type == BuiltInType.BuiltInConstant
                    ? EnumConversions.ToAudioKind(val.AsBuiltInConstant()!.Value)
                    : UnexpectedType<NsAudioKind>(val.Type);
            }

            public NsEntityAction TakeEntityAction()
            {
                ConstantValue val = TakeOpt(ConstantValue.Null);
                return val.Type == BuiltInType.BuiltInConstant
                    ? EnumConversions.ToEntityAction(val.AsBuiltInConstant()!.Value)
                    : UnexpectedType<NsEntityAction>(val.Type);
            }

            public NsRational TakeRational(float denominator = 1000.0f)
            {
                return new NsRational(TakeInt(), denominator);
            }

            public NsNumeric TakeNumeric()
            {
                ConstantValue val = Take();
                return val.Type switch
                {
                    BuiltInType.Integer => new NsNumeric(val.AsInteger()!.Value, isDelta: false),
                    BuiltInType.DeltaInteger => new NsNumeric(val.AsDelta()!.Value, isDelta: true),
                    _ => UnexpectedType<NsNumeric>(val.Type)
                };
            }

            public TimeSpan TakeAnimDelay(TimeSpan animDuration)
            {
                int delayArg = TakeInt();
                return delayArg == 1 ? animDuration : TimeSpan.FromMilliseconds(delayArg);
            }

            public BuiltInConstant TakeConstant()
            {
                ConstantValue val = TakeOpt(ConstantValue.Null);
                return val.Type == BuiltInType.BuiltInConstant
                    ? val.AsBuiltInConstant()!.Value
                    : UnexpectedType<BuiltInConstant>(val.Type);
            }

            public CompositeBezier TakeBezierCurve()
            {
                ConstantValue val = TakeOpt(ConstantValue.Null);
                return val.Type == BuiltInType.BezierCurve
                    ? val.AsBezierCurve()!.Value
                    : UnexpectedType<CompositeBezier>(val.Type);
            }

            public T UnexpectedType<T>(BuiltInType type)
                => throw new NsxCallDispatchException(_pos - 1, type);
        }
    }
}
