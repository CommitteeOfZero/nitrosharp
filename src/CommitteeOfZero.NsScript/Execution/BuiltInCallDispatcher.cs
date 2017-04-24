using System;
using System.Collections.Generic;

namespace CommitteeOfZero.NsScript.Execution
{
    public sealed class BuiltInCallDispatcher
    {
        private const int NssMaxOpacity = 1000;
        private const int NssMaxVolume = 1000;

        private readonly Dictionary<string, Action<ArgumentStack>> _dispatchTable;
        private readonly BuiltInFunctionsBase _builtinsImpl;

        internal ConstantValue Result { get; private set; }

        public BuiltInCallDispatcher(BuiltInFunctionsBase builtinFunctions)
        {
            _builtinsImpl = builtinFunctions;
            _dispatchTable = new Dictionary<string, Action<ArgumentStack>>
            {
                ["Wait"] = Wait,
                ["WaitKey"] = WaitKey,
                ["Request"] = Request,
                ["Delete"] = Delete,
                ["SetAlias"] = SetAlias,
                ["CreateProcess"] = CreateProcess,
                ["LoadImage"] = LoadImage,
                ["CreateColor"] = CreateColor,
                ["CreateTexture"] = CreateTexture,
                ["CreateClipTexture"] = CreateClipTexture,
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
                ["ImageHorizon"] = ImageHorizon,
                ["Random"] = Random,
                ["SoundAmplitude"] = SoundAmplitude
            };
        }

        public void DispatchBuiltInCall(BuiltInFunctionCall functionCall)
        {
            Result = null;
            Action<ArgumentStack> handler;
            _dispatchTable.TryGetValue(functionCall.FunctionName, out handler);
            handler?.Invoke(functionCall.MutableArguments);
        }

        private void DisplayDialogue(ArgumentStack args)
        {
            var text = args.PopString();
            _builtinsImpl.DisplayDialogue(text);
        }

        private void Wait(ArgumentStack args)
        {
            TimeSpan delay = args.PopTimeSpan();
            _builtinsImpl.Delay(delay);
        }

        private void WaitKey(ArgumentStack args)
        {
            if (args.Count > 0)
            {
                TimeSpan timeout = args.PopTimeSpan();
                _builtinsImpl.WaitForInput(timeout);
            }
            else
            {
                _builtinsImpl.WaitForInput();
            }
        }

        private void SetAlias(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            string alias = PreprocessEntityName(args.PopString());

            _builtinsImpl.SetAlias(entityName, alias);
        }

        private void CreateProcess(ArgumentStack args)
        {
            string name = args.PopString();
            args.Pop();
            args.Pop();
            args.Pop();
            string target = args.PopString();

            _builtinsImpl.CreateThread(name, target);
        }

        private void Request(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            NsEntityAction action = args.PopNssAction();

            _builtinsImpl.Request(entityName, action);
        }

        private void Delete(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            _builtinsImpl.RemoveEntity(entityName);
        }

        private void LoadImage(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            string fileName = args.PopString();

            _builtinsImpl.LoadImage(entityName, fileName);
        }

        private void CreateTexture(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            int priority = args.PopInt();
            NsCoordinate x = args.PopCoordinate();
            NsCoordinate y = args.PopCoordinate();
            string fileOrEntityName = PreprocessEntityName(args.PopString());

            _builtinsImpl.AddTexture(entityName, priority, x, y, fileOrEntityName);
        }

        private void CreateClipTexture(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            int priority = args.PopInt();
            NsCoordinate x1 = args.PopCoordinate();
            NsCoordinate y1 = args.PopCoordinate();
            NsCoordinate x2 = args.PopCoordinate();
            NsCoordinate y2 = args.PopCoordinate();
            int width = args.PopInt();
            int height = args.PopInt();
            string srcEntityName = args.PopString();

            _builtinsImpl.AddClippedTexture(entityName, priority, x1, y1, x2, y2, width, height, srcEntityName);
        }

        private void CreateSound(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            NsAudioKind kind = args.PopAudioKind();
            string fileName = args.PopString();
            _builtinsImpl.LoadAudio(entityName, kind, fileName);
        }

        private void CreateColor(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            int priority = args.PopInt();
            NsCoordinate x = args.PopCoordinate();
            NsCoordinate y = args.PopCoordinate();
            int width = args.PopInt();
            int height = args.PopInt();
            NsColor color = args.PopColor();

            _builtinsImpl.AddRectangle(entityName, priority, x, y, width, height, color);
        }

        private void SetVolume(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            TimeSpan duration = args.PopTimeSpan();
            NsRational volume = new NsRational(args.PopInt(), NssMaxVolume);

            _builtinsImpl.SetVolume(entityName, duration, volume);
        }

        private void Fade(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            TimeSpan duration = args.PopTimeSpan();
            var opacity = new NsRational(args.PopInt(), NssMaxOpacity);

            // Unknown. Usually null.
            args.Pop();

            bool wait = args.PopBool();
            _builtinsImpl.Fade(entityName, duration, opacity, wait);
        }

        private void Move(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            TimeSpan duration = args.PopTimeSpan();
            NsCoordinate x = args.PopCoordinate();
            NsCoordinate y = args.PopCoordinate();
            NsEasingFunction easingFunction = args.PopEasingFunction();
            bool wait = args.PopBool();

            _builtinsImpl.Move(entityName, duration, x, y, easingFunction, wait);
        }

        private void Zoom(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            TimeSpan duration = args.PopTimeSpan();
            var scaleX = new NsRational(args.PopInt(), 1000);
            var scaleY = new NsRational(args.PopInt(), 1000);
            NsEasingFunction easingFunction = args.PopEasingFunction();
            bool wait = args.PopBool();

            _builtinsImpl.Zoom(entityName, duration, scaleX, scaleY, easingFunction, wait);
        }

        private void CreateWindow(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            int priority = args.PopInt();
            NsCoordinate x = args.PopCoordinate();
            NsCoordinate y = args.PopCoordinate();
            int width = args.PopInt();
            int height = args.PopInt();

            _builtinsImpl.CreateDialogueBox(entityName, priority, x, y, width, height);
        }

        private void WaitText(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            TimeSpan time = args.PopTimeSpan();

            _builtinsImpl.WaitText(entityName, time);
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
            NsColor inColor = args.PopColor();
            NsColor outColor = args.PopColor();
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
            string entityName = PreprocessEntityName(args.PopString());
            bool looping = args.PopBool();

            _builtinsImpl.ToggleLooping(entityName, looping);
        }

        private void SetLoopPoint(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            TimeSpan loopStart = args.PopTimeSpan();
            TimeSpan loopEnd = args.PopTimeSpan();

            _builtinsImpl.SetLoopPoint(entityName, loopStart, loopEnd);
        }

        private void DrawTransition(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            TimeSpan duration = args.PopTimeSpan();
            var initialOpacity = new NsRational(args.PopInt(), NssMaxOpacity);
            var finalOpacity = new NsRational(args.PopInt(), NssMaxOpacity);
            var feather = new NsRational(args.PopInt(), 100);

            var unk = args.Pop();

            string fileName = args.PopString();
            bool wait = args.PopBool();

            _builtinsImpl.DrawTransition(entityName, duration, initialOpacity, finalOpacity, feather, fileName, wait);
        }

        private static string PreprocessEntityName(string rawEntityName)
        {
            if (string.IsNullOrEmpty(rawEntityName) || rawEntityName.Length < 2)
            {
                return rawEntityName;
            }

            return rawEntityName[0] == '@' ? rawEntityName.Substring(1) : rawEntityName;
        }

        private void ImageHorizon(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());
            int width = _builtinsImpl.GetTextureWidth(entityName);
            Result = new ConstantValue(width, isDelta: false);
        }

        private void RemainTime(ArgumentStack args)
        {
            string entityName = PreprocessEntityName(args.PopString());

            Result = ConstantValue.Zero;
        }

        private void SoundAmplitude(ArgumentStack args)
        {
            string s1 = args.PopString();
            string s2 = args.PopString();

            int amplitude = _builtinsImpl.GetSoundAmplitude();
            Result = new ConstantValue(amplitude, isDelta: false);
        }

        private void Random(ArgumentStack args)
        {
            int max = args.PopInt();

            int n = _builtinsImpl.GenerateRandomNumber(max);
            Result = new ConstantValue(n, isDelta: false);
        }
    }
}
