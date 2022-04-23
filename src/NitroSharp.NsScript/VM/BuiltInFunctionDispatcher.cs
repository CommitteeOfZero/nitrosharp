using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.NsScript.Primitives;

namespace NitroSharp.NsScript.VM
{
    internal sealed class BuiltInFunctionDispatcher
    {
        private readonly ConstantValue[] _variables;
        private BuiltInFunctions _impl;
        private ConstantValue? _result;

        public BuiltInFunctionDispatcher(ConstantValue[] variables)
        {
            _variables = variables;
            _impl = null!;
        }

        private void SetResult(in ConstantValue value)
        {
            Debug.Assert(_result is null);
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
                case BuiltInFunction.Position:
                    Position(ref args);
                    break;
                case BuiltInFunction.CursorPosition:
                    CursorPosition(ref args);
                    break;

                case BuiltInFunction.MoveCursor:
                    MoveCursor(ref args);
                    break;

                case BuiltInFunction.CreateBacklog:
                    CreateBacklog(ref args);
                    break;
                case BuiltInFunction.SetBacklog:
                    SetBacklog(ref args);
                    break;
                case BuiltInFunction.ClearBacklog:
                    ClearBacklog();
                    break;
                case BuiltInFunction.EnableBacklog:
                    EnableBacklog();
                    break;

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
                case BuiltInFunction.Reset:
                    Reset();
                    break;

                case BuiltInFunction.CreateCube:
                    CreateCube(ref args);
                    break;
                case BuiltInFunction.CreateChoice:
                    CreateChoice(ref args);
                    break;
                case BuiltInFunction.SetNextFocus:
                    SetNextFocus(ref args);
                    break;
                case BuiltInFunction.LoadColor:
                    LoadColor(ref args);
                    break;
                case BuiltInFunction.LoadImage:
                    LoadImage(ref args);
                    break;
                case BuiltInFunction.CreateColor:
                    CreateColor(ref args);
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
                case BuiltInFunction.Shake:
                    Shake(ref args);
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
                case BuiltInFunction.SetFont:
                    SetFont(ref args);
                    break;
                case BuiltInFunction.WaitText:
                    WaitText(ref args);
                    break;

                case BuiltInFunction.CreateSound:
                    CreateSound(ref args);
                    break;
                case BuiltInFunction.CreateMovie:
                    CreateMovie(ref args);
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
                case BuiltInFunction.WaitPlay:
                    WaitPlay(ref args);
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
                case BuiltInFunction.DateTime:
                    DateTime(ref args);
                    break;

                case BuiltInFunction.CreateScrollbar:
                    CreateScrollbar(ref args);
                    break;
                case BuiltInFunction.SetScrollbar:
                    SetScrollbar(ref args);
                    break;
                case BuiltInFunction.ScrollbarValue:
                    ScrollbarValue(ref args);
                    break;

                case BuiltInFunction.MountSavedata:
                    MountSavedata(ref args);
                    break;
                case BuiltInFunction.ExistSave:
                    ExistSave(ref args);
                    break;
                case BuiltInFunction.Save:
                    Save(ref args);
                    break;
                case BuiltInFunction.Load:
                    Load(ref args);
                    break;
                case BuiltInFunction.DeleteSaveFile:
                    DeleteSaveFile(ref args);
                    break;
                case BuiltInFunction.AvailableFile:
                    AvailableFile(ref args);
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
                case BuiltInFunction.XBOX360_IsAchieved:
                    XBOX360_IsAchieved(ref args);
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

                case BuiltInFunction.Exit:
                    Exit();
                    break;
            }

            ConstantValue? result = _result;
            _result = null;
            return result;
        }

        private void Shake(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            TimeSpan duration = args.TakeTimeSpan();
            _impl.Shake(
                query,
                duration,
                startX: args.TakeCoordinate(),
                startY: args.TakeCoordinate(),
                endX: args.TakeCoordinate(),
                endY: args.TakeCoordinate(),
                freq: args.TakeUInt(),
                args.TakeEaseFunction(),
                args.TakeAnimDelay(duration)
            );
        }

        private void Reset()
        {
            _impl.Reset();
        }

        private void CreateMovie(ref ArgConsumer args)
        {
            _impl.PlayVideo(
                args.TakeEntityPath(),
                priority: args.TakeInt(),
                x: args.TakeCoordinate(),
                y: args.TakeCoordinate(),
                loop: args.TakeBool(),
                alpha: args.TakeBool(),
                source: args.TakeString()
            );
        }

        private void WaitPlay(ref ArgConsumer args)
        {
            _impl.WaitPlay(args.TakeEntityPath());
        }

        private void Load(ref ArgConsumer args)
        {
            _impl.LoadGame(slot: args.TakeUInt());
        }

        private void AvailableFile(ref ArgConsumer args)
        {
            _result = ConstantValue.Boolean(
                _impl.FileExists(path: args.TakeString())
            );
        }

        private void MountSavedata(ref ArgConsumer args)
        {
            _result = ConstantValue.Boolean(
                _impl.MountSaveData(slot: args.TakeUInt())
            );
        }

        private void ExistSave(ref ArgConsumer args)
        {
            _result = ConstantValue.Boolean(
                _impl.SaveExists(slot: args.TakeUInt())
            );
        }

        private void Save(ref ArgConsumer args)
        {
            _impl.SaveGame(slot: args.TakeUInt());
        }

        private void DeleteSaveFile(ref ArgConsumer args)
        {
            _impl.DeleteSave(slot: args.TakeUInt());
        }

        private void CreateCube(ref ArgConsumer args)
        {
            _impl.CreateCube(
                args.TakeEntityPath(),
                priority: args.TakeInt(),
                args.TakeString(),
                args.TakeString(),
                args.TakeString(),
                args.TakeString(),
                args.TakeString(),
                args.TakeString()
            );
        }

        private void CursorPosition(ref ArgConsumer args)
        {
            short xSlot = args.TakeRef();
            short ySlot = args.TakeRef();
            Vector2 position = _impl.GetCursorPosition();
            _variables[xSlot] = ConstantValue.Number((int)position.X);
            _variables[ySlot] = ConstantValue.Number((int)position.Y);
        }

        private void Position(ref ArgConsumer args)
        {
            EntityPath path = args.TakeEntityPath();
            short xSlot = args.TakeRef();
            short ySlot = args.TakeRef();
            Vector2 position = _impl.GetPosition(path);
            _variables[xSlot] = ConstantValue.Number((int)position.X);
            _variables[ySlot] = ConstantValue.Number((int)position.Y);
        }

        private void SetScrollbar(ref ArgConsumer args)
        {
            _impl.SetScrollbar(scrollbar: args.TakeEntityPath(), parent: args.TakeEntityPath());
        }

        private void CreateBacklog(ref ArgConsumer args)
        {
            _impl.CreateBacklog(args.TakeEntityPath(), priority: args.TakeInt());
        }

        private void SetBacklog(ref ArgConsumer args)
        {
            string text = args.TakeString();
            args.Take();
            args.Take();
            _impl.SetBacklog(text);
        }

        private void ClearBacklog()
        {
            _impl.ClearBacklog();
        }

        private void MoveCursor(ref ArgConsumer args)
        {
            _impl.MoveCursor(x: args.TakeInt(), y: args.TakeInt());
        }

        private void XBOX360_IsAchieved(ref ArgConsumer args)
        {
            args.TakeInt();
            _result = ConstantValue.True;
        }

        private void EnableBacklog()
        {
            _result = ConstantValue.True;
        }

        private void Exit()
        {
            _impl.Exit();
        }

        private void SetFont(ref ArgConsumer args)
        {
            _impl.SetFont(
                family: args.TakeString(),
                size: args.TakeInt(),
                color: args.TakeColor(),
                outlineColor: args.TakeColor(),
                weight: NsFontWeight.From(args.Take()),
                outlineOffset: args.TakeOutlineOffset()
            );
        }

        private void CreateScrollbar(ref ArgConsumer args)
        {
            _impl.CreateScrollbar(
                args.TakeEntityPath(),
                priority: args.TakeInt(),
                x1: args.TakeInt(),
                y1: args.TakeInt(),
                x2: args.TakeInt(),
                y2: args.TakeInt(),
                initialValue: args.TakeRational(),
                scrollDirection: EnumConversions.ToScrollDirection(args.TakeConstant()),
                knobImage: args.TakeString()
            );
        }

        private void ScrollbarValue(ref ArgConsumer args)
        {
            EntityPath scrollbarEntity = args.TakeEntityPath();
            SetResult(ConstantValue.Number(_impl.GetScrollbarValue(scrollbarEntity)));
        }

        private void WaitMove(ref ArgConsumer args)
        {
            _impl.WaitMove(args.TakeEntityQuery());
        }

        private void WaitAction(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            TimeSpan ts = args.TakeTimeSpan();
            TimeSpan? tsOpt = ts > TimeSpan.Zero ? ts : (TimeSpan?)null;
            _impl.WaitAction(query, tsOpt);
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
                imagePath: args.TakeString(),
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
               markup: args.TakeString()
            );
        }

        private void Integer(ref ArgConsumer args)
        {
            ConstantValue value = args.TakeOpt(ConstantValue.Null);
            switch (value.Type)
            {
                case BuiltInType.Numeric:
                    value = ConstantValue.Number((int)value.AsNumber()!.Value);
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

        private void LoadText(ref ArgConsumer args)
        {
            string subroutineName = args.TakeString();
            string boxName = args.TakeString();
            string blockName = args.TakeString();
            uint maxWidth = args.TakeUInt();
            uint maxHeight = args.TakeUInt();
            int letterSpacing = args.TakeInt();
            int lineSpacing = args.TakeInt();

            if (blockName.StartsWith('@'))
            {
                blockName = blockName[1..];
            }

            NsxModule module = _impl.CurrentThread.CallFrameStack.Peek(1).Module;
            int sub = module.LookupSubroutineIndex(subroutineName);
            ref readonly SubroutineRuntimeInfo srti = ref module.GetSubroutineRuntimeInfo(sub);
            int blockIndex = srti.LookupDialogueBlockIndex(blockName);
            int codeOffset = module.GetSubroutine(sub).DialogueBlockOffsets[blockIndex];

            var token = new DialogueBlockToken(boxName, blockName, module, sub, codeOffset);
            _impl.LoadDialogueBlock(token, maxWidth, maxHeight, letterSpacing, lineSpacing);
        }

        private void WaitText(ref ArgConsumer args)
        {
            _impl.WaitText(args.TakeEntityQuery(), args.TakeTimeSpan());
        }

        private void SetVolume(ref ArgConsumer args)
        {
            _impl.SetVolume(
                args.TakeEntityQuery(),
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
                args.TakeEntityQuery(),
                enable: args.TakeBool()
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
            NsCoordinate dstX = args.TakeCoordinate();
            NsCoordinate dstY = args.TakeCoordinate();
            NsEaseFunction easeFunction = NsEaseFunction.Linear;
            TimeSpan delay = default;
            if (args.Count == 6)
            {
                easeFunction = args.TakeEaseFunction();
                delay = args.TakeAnimDelay(duration);
            }
            _impl.Move(
                query,
                duration,
                dstX, dstY,
                easeFunction,
                delay
            );
        }

        private void Fade(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            if (args.Count == 3)
            {
                _impl.Fade(
                    query,
                    duration: TimeSpan.Zero,
                    dstOpacity: args.TakeRational(),
                    NsEaseFunction.Linear,
                    TimeSpan.Zero
                );
                return;
            }
            else
            {
                TimeSpan duration = args.TakeTimeSpan();
                NsRational dstOpacity = args.TakeRational();
                NsEaseFunction easeFunction = NsEaseFunction.Linear;
                TimeSpan animDelay = default;
                if (args.Count == 4)
                {
                    animDelay = args.TakeAnimDelay(duration);
                }
                else
                {

                    if (args.AsSpan(0)[^1].AsBuiltInConstant() is null)
                    {
                        easeFunction = args.TakeEaseFunction();
                        animDelay = args.TakeAnimDelay(duration);
                    }
                    else
                    {
                        // Sometimes the order is switched.
                        animDelay = args.TakeAnimDelay(duration);
                        easeFunction = args.TakeEaseFunction();
                    }
                }

                _impl.Fade(
                    query,
                    duration,
                    dstOpacity,
                    easeFunction,
                    animDelay
                );
            }
        }

        private void DrawTransition(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            TimeSpan duration = args.TakeTimeSpan();
            _impl.BeginTransition(
                query,
                duration,
                srcFadeAmount: args.TakeRational(),
                dstFadeAmount: args.TakeRational(),
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

        private void SetNextFocus(ref ArgConsumer args)
        {
            _impl.SetNextFocus(
                first: args.TakeEntityPath(),
                second: args.TakeEntityPath(),
                args.TakeFocusDirection()
            );
        }

        private void LoadColor(ref ArgConsumer args)
        {
            _impl.LoadColor(
                args.TakeEntityPath(),
                width: args.TakeUInt(),
                height: args.TakeUInt(),
                args.TakeColor()
            );
        }

        private void LoadImage(ref ArgConsumer args)
        {
            _impl.LoadImage(
                args.TakeEntityPath(),
                source: args.TakeString()
            );
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
                source: args.TakeString()
            );
        }

        private void Time()
        {
            SetResult(ConstantValue.Number(_impl.GetSecondsElapsed()));
        }

        private void DateTime(ref ArgConsumer args)
        {
            short yearSlot = args.TakeRef();
            short monthSlot = args.TakeRef();
            short daySlot = args.TakeRef();
            short hourSlot = args.TakeRef();
            short minuteSlot = args.TakeRef();
            short secondSlot = args.TakeRef();
            DateTime now = _impl.GetDateTime();
            _variables[yearSlot] = ConstantValue.Number(now.Year);
            _variables[monthSlot] = ConstantValue.Number(now.Month);
            _variables[daySlot] = ConstantValue.Number(now.Day);
            _variables[hourSlot] = ConstantValue.Number(now.Hour);
            _variables[minuteSlot] = ConstantValue.Number(now.Minute);
            _variables[secondSlot] = ConstantValue.Number(now.Second);
        }

        private void String(ref ArgConsumer args)
        {
            string format = args.TakeString();
            var list = new List<object>();
            foreach (ref readonly ConstantValue arg in args.AsSpan(1))
            {
                int? num = (int?)arg.AsNumber();
                if (num is not null)
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
            SetResult(ConstantValue.Number(_impl.GetPlatformId()));
        }

        private void SoundAmplitude(ref ArgConsumer args)
        {
            string something = args.TakeString();
            string characterName = args.TakeString();
            SetResult(ConstantValue.Number(_impl.GetSoundAmplitude(characterName)));
        }

        private void Random(ref ArgConsumer args)
        {
            int max = args.TakeInt();
            SetResult(ConstantValue.Number(_impl.GetRandomNumber(max)));
        }

        private void ImageHorizon(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            SetResult(ConstantValue.Number(_impl.GetWidth(entityPath)));
        }

        private void ImageVertical(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            SetResult(ConstantValue.Number(_impl.GetHeight(entityPath)));
        }

        private void RemainTime(ref ArgConsumer args)
        {
            EntityQuery query = args.TakeEntityQuery();
            SetResult(ConstantValue.Number(_impl.GetTimeRemaining(query)));
        }

        private void PassageTime(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            SetResult(ConstantValue.Number(_impl.GetTimeElapsed(entityPath)));
        }

        private void DurationTime(ref ArgConsumer args)
        {
            EntityPath entityPath = args.TakeEntityPath();
            SetResult(ConstantValue.Number(_impl.GetMediaDuration(entityPath)));
        }

        private void XBOX360_AwardGameIcon()
        {
        }

        private void XBOX360_InitUser()
        {
            _impl.X360_InitUser();
        }

        private void XBOX360_SelectStorage(ref ArgConsumer args)
        {
            int storage = args.TakeInt();
            _result = ConstantValue.True;
        }

        private void XBOX360_PadTrigger(ref ArgConsumer args)
        {
            XboxTrigger trigger = args.TakeInt() switch
            {
                0 => XboxTrigger.Left,
                1 => XboxTrigger.Right,
                _ => XboxTrigger.Unknown
            };
            _result = ConstantValue.Number(_impl.X360_GetTriggerAxis(trigger));
        }

        private void XBOX360_ExistContent(ref ArgConsumer args)
        {
            _result = ConstantValue.Boolean(_impl.X360_UserDataExists());
        }

        private void XBOX360_StorageSize(ref ArgConsumer args)
        {
            _result = ConstantValue.Number(int.MaxValue);
        }

        private void XBOX360_CurrentStorage(ref ArgConsumer args)
        {
            _result = ConstantValue.Number(0);
        }

        private void XBOX360_UserIndex(ref ArgConsumer args)
        {
            _result = ConstantValue.Number(0);
        }

        private void XBOX360_CheckStorage(ref ArgConsumer args)
        {
            _result = ConstantValue.Boolean(_impl.X360_CheckStorage());
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
            _result = ConstantValue.Boolean(_impl.X360_IsSignedIn());
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

            private static TimeSpan Time(float ms) => TimeSpan.FromMilliseconds(ms);

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

            public EntityPath TakeEntityPath() => new(TakeString());
            public EntityQuery TakeEntityQuery() => new(TakeString());

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
                ConstantValue arg = Take();
                return arg.Type switch
                {
                    BuiltInType.Numeric => (int)arg.AsNumber()!.Value,
                    BuiltInType.String when int.TryParse(arg.AsString()!, out int val) => val,
                    BuiltInType.Null => 0,
                    _ => UnexpectedType<int>(arg.Type)
                };
            }

            public TimeSpan TakeTimeSpan()
            {
                ConstantValue val = TakeOpt(ConstantValue.Number(0));
                float num = val.Type switch
                {
                    BuiltInType.Numeric => val.AsNumber()!.Value,
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
                    BuiltInType.Numeric => NsColor.FromRgb((int)val.AsNumber()!.Value),
                    BuiltInType.BuiltInConstant => NsColor.FromConstant(val.AsBuiltInConstant()!.Value),
                    _ => UnexpectedType<NsColor>(val.Type)
                };
            }

            public NsCoordinate TakeCoordinate()
            {
                ConstantValue val = TakeOpt(ConstantValue.Number(0));
                NsCoordinate? ret = NsCoordinate.FromValue(val);
                return ret ?? UnexpectedType<NsCoordinate>(val.Type);
            }

            public NsTextDimension TakeDimension()
            {
                ConstantValue val = TakeOpt(ConstantValue.Number(0));
                return val.Type switch
                {
                    BuiltInType.Numeric
                        => NsTextDimension.WithValue((int)val.AsNumber()!.Value),
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
                    BuiltInType.Null => NsEaseFunction.Linear,
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

            public NsOutlineOffset TakeOutlineOffset()
            {
                ConstantValue val = Take();
                return val.Type switch
                {
                    BuiltInType.BuiltInConstant
                        => EnumConversions.ToOutlineOffset(val.AsBuiltInConstant()!.Value),
                    BuiltInType.Null => NsOutlineOffset.Unspecified,
                    _ => UnexpectedType<NsOutlineOffset>(val.Type)
                };
            }

            public NsRational TakeRational(float denominator = 1000.0f)
            {
                return new(TakeInt(), denominator);
            }

            public NsNumeric TakeNumeric()
            {
                ConstantValue val = Take();
                return val.Type switch
                {
                    BuiltInType.Numeric => new NsNumeric(val.AsNumber()!.Value, isDelta: false),
                    BuiltInType.DeltaNumeric => new NsNumeric(val.AsDeltaNumber()!.Value, isDelta: true),
                    _ => UnexpectedType<NsNumeric>(val.Type)
                };
            }

            public TimeSpan TakeAnimDelay(TimeSpan animDuration)
            {
                ConstantValue val = Take();
                return (val.Type, val.AsNumber()) switch
                {
                    (BuiltInType.Numeric or BuiltInType.Boolean, float delay) =>
                        (int)delay == 1 ? animDuration : TimeSpan.FromMilliseconds(delay),
                    (BuiltInType.Null, _) => TimeSpan.FromSeconds(0),
                    _ => UnexpectedType<TimeSpan>(val.Type)
                };
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

            public NsFocusDirection TakeFocusDirection()
            {
                ConstantValue val = Take();
                return val.Type == BuiltInType.BuiltInConstant
                    ? EnumConversions.ToFocusDirection(val.AsBuiltInConstant()!.Value)
                    : UnexpectedType<NsFocusDirection>(val.Type);
            }

            public short TakeRef()
            {
                ConstantValue val = Take();
                return val.GetSlotInfo(out short slot)
                    ? slot
                    : throw new Exception("ConstantValue doesn't contain slot information. TODO: better error handling");
            }
        }
    }
}
