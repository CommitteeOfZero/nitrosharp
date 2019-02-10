using System;
using System.Runtime.CompilerServices;

namespace NitroSharp.NsScriptNew.VM
{
    public class BuiltInFunctionDispatcher
    {
        private readonly ConstantValue[] _argBuffer = new ConstantValue[16];
        private readonly BuiltInFunctions _impl;

        public BuiltInFunctionDispatcher(BuiltInFunctions functionsImpl)
        {
            _impl = functionsImpl;
        }

        public void Dispatch(BuiltInFunction function, ReadOnlySpan<ConstantValue> args)
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
            }
        }

        private void SetAlias(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 2);
            string entityName = AssertString(args, 0);
            string alias = AssertString(args, 1);
            _impl.SetAlias(entityName, alias);
        }

        private void Request(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 2);
            string entityName = AssertString(args, 0);
            BuiltInConstant constant = AssertBuiltInConstant(args, 1);
            NsEntityAction action = EnumConversions.ToEntityAction(constant);
            _impl.Request(entityName, action);
        }

        private void Delete(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 1);
            string entityName = AssertString(args, 0);
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
            string entityName = AssertString(args, 0);
            _impl.CreateChoice(entityName);
        }

        private void CreateColor(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 7);
            string entityName = AssertString(args, 0);
            int priority = AssertInteger(args, 1);
            int width = AssertInteger(args, 4);
            int height = AssertInteger(args, 5);

            _impl.FillRectangle(entityName, priority, default, default, width, height, default);
        }

        private void LoadImage(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 2);
            string entityName = AssertString(args, 0);
            string fileName = AssertString(args, 1);
            _impl.LoadImage(entityName, fileName);
        }

        private void CreateTexture(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 5);
            string entityName = AssertString(args, 0);
            int priority = AssertInteger(args, 1);
            string fileOrEntityName = AssertString(args, 4);
            _impl.CreateSprite(entityName, priority, default, default, fileOrEntityName);
        }

        private void CreateClipTexture(ReadOnlySpan<ConstantValue> args)
        {
            args = AssertArgs(args, countRequired: 5);
            string entityName = AssertString(args, 0);
            int priority = AssertInteger(args, 1);
            int width = AssertInteger(args, 6);
            int height = AssertInteger(args, 7);
            string srcEntityName = AssertString(args, 8);
            _impl.CreateSpriteEx(entityName, priority, default, default, default, default, width, height, srcEntityName);
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
                        _argBuffer[i] = ConstantValue.Null;
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
