using System;
using System.Collections.Generic;
using System.Text;

namespace SciAdvNet.NSScript.Execution
{
    public sealed class BuiltInCallDispatcher
    {
        private const int NssMaxOpacity = 1000;

        private readonly Dictionary<string, Action<ArgumentStack>> _dispatchTable;
        private readonly NssImplementation _nssImpl;

        public BuiltInCallDispatcher(NssImplementation nssImplementation)
        {
            _nssImpl = nssImplementation;
            _dispatchTable = new Dictionary<string, Action<ArgumentStack>>
            {
                ["Wait"] = Wait,
                ["WaitKey"] = WaitKey,
                ["Request"] = Request,
                ["Delete"] = Delete,
                ["SetAlias"] = SetAlias,
                ["CreateColor"] = CreateColor,
                ["CreateTexture"] = CreateTexture,
                ["CreateSound"] = CreateSound,
                ["Fade"] = Fade,
                ["Move"] = Move,
                ["Zoom"] = Zoom,
                ["SetVolume"] = SetVolume,
                ["CreateWindow"] = CreateWindow,
                ["LoadText"] = LoadText,
                ["WaitText"] = WaitText,
                ["SetLoop"] = SetLoop,
                ["SetLoopPoint"] = SetLoopPoint,
                ["DrawTransition"] = DrawTransition,
                ["DisplayDialogue"] = DisplayDialogue,

                ["RemainTime"] = RemainTime,
                ["ImageHorizon"] = ImageHorizon
            };
        }

        private void ImageHorizon(ArgumentStack args)
        {
            string entityName = args.PopString();
            _nssImpl.CurrentThread.CurrentFrame.EvaluationStack.Push(new ConstantValue(1200));
        }

        public void DispatchBuiltInCall(BuiltInFunctionCall functionCall)
        {
            Action<ArgumentStack> handler;
            _dispatchTable.TryGetValue(functionCall.FunctionName, out handler);
            handler?.Invoke(functionCall.MutableArguments);
        }

        private void DisplayDialogue(ArgumentStack args)
        {
            var text = args.PopString();
            _nssImpl.DisplayDialogue(text);
        }

        private void Wait(ArgumentStack args)
        {
            TimeSpan delay = args.PopTimeSpan();
            _nssImpl.Delay(delay);
        }

        private void WaitKey(ArgumentStack args)
        {
            if (args.Count > 0)
            {
                TimeSpan timeout = args.PopTimeSpan();
                _nssImpl.WaitForInput(timeout);
            }
            else
            {
                _nssImpl.WaitForInput();
            }
        }

        private void SetAlias(ArgumentStack args)
        {
            string entityName = args.PopString();
            string alias = args.PopString();

            _nssImpl.SetAlias(entityName, alias);
        }

        private void Request(ArgumentStack args)
        {
            string entityName = args.PopString();
            NssEntityAction action = args.PopNssAction();

            _nssImpl.Request(entityName, action);
        }

        private void Delete(ArgumentStack args)
        {
            string entityName = args.PopString();
            _nssImpl.RemoveEntity(entityName);
        }

        private void CreateTexture(ArgumentStack args)
        {
            string entityName = args.PopString();
            int priority = args.PopInt();
            NssCoordinate x = args.PopCoordinate();
            NssCoordinate y = args.PopCoordinate();
            string fileOrEntityName = args.PopString();

            _nssImpl.AddTexture(entityName, priority, x, y, fileOrEntityName);
        }

        private void CreateSound(ArgumentStack args)
        {
            string entityName = args.PopString();
            string strAudioKind = args.PopString();
            AudioKind kind;
            switch (strAudioKind)
            {
                case "SE":
                    kind = AudioKind.SoundEffect;
                    break;

                case "BGM":
                default:
                    kind = AudioKind.BackgroundMusic;
                    break;
            }

            string fileName = args.PopString();
            _nssImpl.LoadAudio(entityName, kind, fileName);
        }

        private void CreateColor(ArgumentStack args)
        {
            string entityName = args.PopString();
            int priority = args.PopInt();
            NssCoordinate x = args.PopCoordinate();
            NssCoordinate y = args.PopCoordinate();
            int width = args.PopInt();
            int height = args.PopInt();
            NssColor color = args.PopColor();

            _nssImpl.AddRectangle(entityName, priority, x, y, width, height, color);
        }

        private void SetVolume(ArgumentStack args)
        {
            string entityName = args.PopString();
            TimeSpan duration = args.PopTimeSpan();
            int volume = args.PopInt();

            _nssImpl.SetVolume(entityName, duration, volume);
        }

        private void Fade(ArgumentStack args)
        {
            string entityName = args.PopString();
            TimeSpan duration = args.PopTimeSpan();
            var opacity = new Rational(args.PopInt(), NssMaxOpacity);

            // Unknown. Usually null.
            args.Pop();

            bool wait = args.PopBool();
            _nssImpl.Fade(entityName, duration, opacity, wait);
        }

        private void Move(ArgumentStack args)
        {
            string entityName = args.PopString();
            TimeSpan duration = args.PopTimeSpan();
            NssCoordinate x = args.PopCoordinate();
            NssCoordinate y = args.PopCoordinate();
            EasingFunction easingFunction = args.PopEasingFunction();
            bool wait = args.PopBool();

            _nssImpl.Move(entityName, duration, x, y, easingFunction, wait);
        }

        private void Zoom(ArgumentStack args)
        {
            string entityName = args.PopString();
            TimeSpan duration = args.PopTimeSpan();
            var scaleX = new Rational(args.PopInt(), 1000);
            var scaleY = new Rational(args.PopInt(), 1000);
            EasingFunction easingFunction = args.PopEasingFunction();
            bool wait = args.PopBool();

            _nssImpl.Zoom(entityName, duration, scaleX, scaleY, easingFunction, wait);
        }

        private void CreateWindow(ArgumentStack args)
        {
            string entityName = args.PopString();
            int priority = args.PopInt();
            NssCoordinate x = args.PopCoordinate();
            NssCoordinate y = args.PopCoordinate();
            int width = args.PopInt();
            int height = args.PopInt();

            _nssImpl.CreateDialogueBox(entityName, priority, x, y, width, height);
        }

        private void WaitText(ArgumentStack args)
        {
            string entityName = args.PopString();
            TimeSpan time = args.PopTimeSpan();

            _nssImpl.WaitText(entityName, time);
        }

        private void LoadText(ArgumentStack args)
        {
            string unk = args.PopString();
            string boxName = args.PopString();
            string someStr = args.PopString();

            int maxWidth = args.PopInt();
            int maxHeight = args.PopInt();
            int letterSpacing = args.PopInt();
            int lineSpacing = args.PopInt();
        }

        private void SetFont(ArgumentStack args)
        {
            string fontName = args.PopString();
            int size = args.PopInt();
            NssColor inColor = args.PopColor();
            NssColor outColor = args.PopColor();
            int fontWeight = args.PopInt();

            string strAlignment = args.PopString();
            //TextAlignment alignment;
            //switch (strAlignment.ToUpperInvariant())
            //{
            //    case "DOWN":
            //    default:
            //        alignment = TextAlignment.Bottom;
            //        break;
            //}
        }

        private void SetLoop(ArgumentStack args)
        {
            string entityName = args.PopString();
            bool looping = args.PopBool();

            _nssImpl.ToggleLooping(entityName, looping);
        }

        private void SetLoopPoint(ArgumentStack args)
        {
            string entityName = args.PopString();
            TimeSpan loopStart = args.PopTimeSpan();
            TimeSpan loopEnd = args.PopTimeSpan();

            _nssImpl.SetLoopPoint(entityName, loopStart, loopEnd);
        }

        private void DrawTransition(ArgumentStack args)
        {
            string entityName = args.PopString();
            TimeSpan duration = args.PopTimeSpan();
            var initialOpacity = new Rational(args.PopInt(), NssMaxOpacity);
            var finalOpacity = new Rational(args.PopInt(), NssMaxOpacity);
            var feather = new Rational(args.PopInt(), 100);

            var unk = args.Pop();

            string fileName = args.PopString();
            bool wait = args.PopBool();

            _nssImpl.DrawTransition(entityName, duration, initialOpacity, finalOpacity, feather, fileName, wait);
        }

        private void RemainTime(ArgumentStack args)
        {
            //string entityName = args.PopString();
            //_currentThread.CurrentFrame.EvaluationStack.Push(new ConstantValue(0));
        }
    }
}
