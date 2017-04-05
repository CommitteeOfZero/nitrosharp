using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace SciAdvNet.NSScript.Execution
{
    public enum NSScriptInterpreterStatus
    {
        Running,
        Suspended,
        Idle
    }

    public class NSScriptInterpreter
    {
        private readonly ExpressionFlattener _exprFlattener;
        private readonly ExpressionReducer _exprReducer;
        private readonly RecursiveExpressionEvaluator _recursiveEvaluator;

        private readonly NssImplementation _nssImplementation;
        private readonly BuiltInCallDispatcher _builtInCallDispatcher;
        private readonly Queue<BuiltInFunctionCall> _pendingBuiltInCalls;

        private ThreadContext _currentThread;
        private readonly List<ThreadContext> _threads;
        private readonly List<ThreadContext> _activeThreads;
        private readonly List<ThreadContext> _suspendedThreads;
        private readonly Queue<ThreadContext> _idleThreads;
        private readonly Queue<ThreadContext> _threadsToResume;

        private readonly Stopwatch _timer;
        private uint _nextThreadId;

        private DialogueBlock _currentDialogueBlock;

        public NSScriptInterpreter(IScriptLocator scriptLocator, NssImplementation nssImplementation)
        {
            _exprFlattener = new ExpressionFlattener();
            _exprReducer = new ExpressionReducer();
            _recursiveEvaluator = new RecursiveExpressionEvaluator();

            _nssImplementation = nssImplementation;
            _builtInCallDispatcher = new BuiltInCallDispatcher(_nssImplementation);
            _pendingBuiltInCalls = new Queue<BuiltInFunctionCall>();

            _threads = new List<ThreadContext>();
            _activeThreads = new List<ThreadContext>();
            _suspendedThreads = new List<ThreadContext>();
            _idleThreads = new Queue<ThreadContext>();
            _threadsToResume = new Queue<ThreadContext>();

            Session = new NSScriptSession(scriptLocator);
            Globals = new VariableTable();

            PredefinedConstants.Preload();

            Status = NSScriptInterpreterStatus.Idle;
            _timer = Stopwatch.StartNew();
        }

        public VariableTable Globals { get; }
        public NSScriptSession Session { get; }
        public NSScriptInterpreterStatus Status { get; private set; }

        public ThreadContext CurrentThread
        {
            get
            {
                if (Status == NSScriptInterpreterStatus.Idle)
                {
                    throw new InvalidOperationException("The interpreter is not running.");
                }

                return _currentThread;
            }
        }

        public event EventHandler<Function> EnteredFunction;
        public event EventHandler<BuiltInFunctionCall> BuiltInCallScheduled;

        public void CreateThread(Module module, Statement entryPoint)
        {
            uint id = _nextThreadId++;
            var thread = new ThreadContext(id, module, entryPoint, Globals);
            _threads.Add(thread);
            _activeThreads.Add(thread);
        }

        public void CreateThread(Module module) => CreateThread(module, module.MainChapter.Body);
        public void CreateThread(string moduleName) => CreateThread(Session.GetModule(moduleName));

        public TimeSpan Run(TimeSpan timeQuota)
        {
            if (_suspendedThreads.Count > 0)
            {
                ProcessSuspendedThreads();
            }

            if (_activeThreads.Count == 0)
            {
                return TimeSpan.Zero;
            }

            Status = NSScriptInterpreterStatus.Running;
            var startTime = _timer.Elapsed;
            while (Status == NSScriptInterpreterStatus.Running && _threads.Count > 0)
            {
                while (_pendingBuiltInCalls.Count > 0)
                {
                    if (IsApproachingQuota(_timer.Elapsed - startTime, timeQuota))
                    {
                        goto exit;
                    }

                    var call = _pendingBuiltInCalls.Dequeue();
                    DispatchBuiltInCall(call);
                }

                while (_idleThreads.Count > 0)
                {
                    var thread = _idleThreads.Dequeue();
                    _activeThreads.Remove(thread);
                    _threads.Remove(thread);
                }

                if (_activeThreads.Count == 0)
                {
                    break;
                }

                foreach (var thread in _activeThreads)
                {
                    if (IsApproachingQuota(_timer.Elapsed - startTime, timeQuota))
                    {
                        goto exit;
                    }

                    _currentThread = thread;
                    Tick();

                    if (thread.DoneExecuting)
                    {
                        _idleThreads.Enqueue(thread);
                    }
                }
            }

            exit:
            return _timer.Elapsed - startTime;
        }

        private void DispatchBuiltInCall(BuiltInFunctionCall call)
        {
            BuiltInCallScheduled?.Invoke(this, call);
            _nssImplementation.CurrentThread = _currentThread;
            _nssImplementation.CurrentDialogueBlock = _currentDialogueBlock;

            _builtInCallDispatcher.DispatchBuiltInCall(call);
        }

        private void Tick()
        {
            Frame prevFrame = _currentThread.CurrentFrame;
            ExecuteNode(_currentThread.CurrentNode);
            if (prevFrame == _currentThread.CurrentFrame && prevFrame.OperationStack.Count == 0)
            {
                _currentThread.Advance();
            }
        }

        private void ExecuteNode(SyntaxNode node)
        {
            switch (node.Kind)
            {
                case SyntaxNodeKind.Block:
                    HandleBlock(node as Block);
                    break;

                case SyntaxNodeKind.ExpressionStatement:
                    HandleExpressionStatement(node as ExpressionStatement);
                    break;

                case SyntaxNodeKind.IfStatement:
                    HandleIf(node as IfStatement);
                    break;

                case SyntaxNodeKind.WhileStatement:
                    HandleWhile(node as WhileStatement);
                    break;

                case SyntaxNodeKind.DialogueBlock:
                    HandleDialogueBlock(node as DialogueBlock);
                    break;

                case SyntaxNodeKind.PXmlString:
                    HandlePXmlString(node as PXmlString);
                    break;
            }
        }

        private void HandleBlock(Block block)
        {
            _currentThread.PushContinuation(block.Statements);
        }

        private void HandleExpressionStatement(ExpressionStatement expressionStatement)
        {
            var expr = expressionStatement.Expression;

            // Fast path for simple function calls.
            if (expr is FunctionCall fc)
            {
                PrepareFunctionCall(fc);
                return;
            }

            Eval(expr, out var _);
        }

        private void HandleIf(IfStatement ifStatement)
        {
            if (Eval(ifStatement.Condition, out var condition))
            {
                if (condition == ConstantValue.True)
                {
                    _currentThread.PushContinuation(ifStatement.IfTrueStatement);
                }
                else if (ifStatement.IfFalseStatement != null)
                {
                    _currentThread.PushContinuation(ifStatement.IfFalseStatement);
                }
            }
        }

        private void HandleWhile(WhileStatement whileStatement)
        {
            if (Eval(whileStatement.Condition, out var condition))
            {
                if (condition.RawValue is 1)
                {
                    _currentThread.PushContinuation(whileStatement.Body);
                }
            }
        }

        private void HandleDialogueBlock(DialogueBlock dialogueBlock)
        {
            var currentFrame = _currentThread.CurrentFrame;
            currentFrame.Globals["SYSTEM_present_preprocess"] = new ConstantValue(dialogueBlock.BoxName);
            currentFrame.Globals["SYSTEM_present_text"] = new ConstantValue(dialogueBlock.Identifier);

            currentFrame.Globals["boxtype"] = new ConstantValue(dialogueBlock.BoxName);
            currentFrame.Globals["textnumber"] = new ConstantValue(dialogueBlock.Identifier);

            currentFrame.Globals["Pretextnumber"] = new ConstantValue("xxx");

            _currentDialogueBlock = dialogueBlock;
        }

        private void HandlePXmlString(PXmlString pxmlString)
        {
            var arg = new ConstantValue(pxmlString.Text);
            ScheduleBuiltInCall("DisplayDialogue", new ArgumentStack(ImmutableArray.Create(arg)));
        }

        private ConstantValue EvaluateTrivial(Expression expression)
        {
            return _recursiveEvaluator.EvaluateExpression(expression, _currentThread.CurrentFrame);
        }

        private bool Eval(Expression expression, out ConstantValue result)
        {
            var currentFrame = _currentThread.CurrentFrame;
            if (currentFrame.EvaluationStack.Count == 0)
            {
                _exprFlattener.Flatten(expression, currentFrame.OperandStack, currentFrame.OperationStack);
            }

            _exprReducer.CurrentFrame = _currentThread.CurrentFrame;
            while (currentFrame.OperationStack.Count > 0)
            {
                var operation = currentFrame.OperationStack.Pop();
                var opCategory = OperationInfo.GetCategory(operation);
                switch (opCategory)
                {
                    case OperationCategory.None:
                        var operand = currentFrame.OperandStack.Pop();

                        if (operand is FunctionCall call)
                        {
                            PrepareFunctionCall(call);
                            result = default(ConstantValue);
                            return false;
                        }

                        currentFrame.EvaluationStack.Push(operand);
                        continue;

                    case OperationCategory.Unary:
                        operand = currentFrame.EvaluationStack.Pop();
                        var intermediateResult = _exprReducer.ApplyUnaryOperation(operand, operation);

                        currentFrame.EvaluationStack.Push(intermediateResult);
                        break;

                    case OperationCategory.Binary:
                        var leftOperand = currentFrame.EvaluationStack.Pop();
                        var rightOperand = currentFrame.EvaluationStack.Pop();

                        var leftReduced = _exprReducer.ReduceExpression(leftOperand);
                        var rightReduced = _exprReducer.ReduceExpression(rightOperand);
                        intermediateResult = _exprReducer.ApplyBinaryOperation(leftReduced, operation, rightReduced);

                        currentFrame.EvaluationStack.Push(intermediateResult);
                        break;

                    case OperationCategory.Assignment:
                        var targetVariable = currentFrame.EvaluationStack.Pop() as Variable;
                        var value = _exprReducer.ReduceExpression(currentFrame.EvaluationStack.Pop());

                        string targetName = targetVariable.Name.SimplifiedName;
                        _currentThread.CurrentFrame.Globals[targetName] = value;
                        currentFrame.EvaluationStack.Push(value);
                        break;
                }
            }

            result = _exprReducer.ReduceExpression(currentFrame.EvaluationStack.Pop());
            return true;
        }

        private void PrepareFunctionCall(FunctionCall functionCall)
        {
            string name = functionCall.TargetFunctionName.SimplifiedName;
            if (!_currentThread.CurrentModule.TryGetFunction(name, out Function target))
            {
                ScheduleBuiltInCall(functionCall);
                return;
            }

            var arguments = new VariableTable();
            for (int i = 0; i < target.Parameters.Length; i++)
            {
                ParameterReference param = target.Parameters[i];
                ConstantValue arg = EvaluateTrivial(functionCall.Arguments[i]);
                arguments[param.ParameterName.SimplifiedName] = arg;
            }

            _currentThread.PushContinuation(target.Body, arguments);
        }

        private void ScheduleBuiltInCall(FunctionCall functionCall)
        {
            string name = functionCall.TargetFunctionName.SimplifiedName;
            var args = new ArgumentStack(functionCall.Arguments.Select(EvaluateTrivial).Reverse());
            ScheduleBuiltInCall(name, args);
        }

        private void ScheduleBuiltInCall(string functionName, ArgumentStack arguments)
        {
            _pendingBuiltInCalls.Enqueue(new BuiltInFunctionCall(functionName, arguments, _currentThread.Id));
        }

        private void ProcessSuspendedThreads()
        {
            var time = _timer.Elapsed;
            foreach (var suspendedThread in _suspendedThreads)
            {
                var delta = time - suspendedThread.SuspensionTime;
                if (delta > suspendedThread.SleepTimeout)
                {
                    _threadsToResume.Enqueue(suspendedThread);
                }
            }

            while (_threadsToResume.Count > 0)
            {
                ResumeThread(_threadsToResume.Dequeue());
            }
        }

        private static bool IsApproachingQuota(TimeSpan elapsedTime, TimeSpan timeQuota)
        {
            if (elapsedTime >= timeQuota)
            {
                return true;
            }

            return (timeQuota - elapsedTime).TotalMilliseconds <= 3.0f;
        }

        public void Suspend()
        {
            Status = NSScriptInterpreterStatus.Suspended;
        }

        public void SuspendThread(uint threadId)
        {
            SuspendThread(threadId, TimeSpan.MaxValue);
        }

        public void SuspendThread(ThreadContext thread)
        {
            SuspendThread(thread, TimeSpan.MaxValue);
        }

        public void SuspendThread(ThreadContext thread, TimeSpan timeout)
        {
            thread.Suspend(_timer.Elapsed, timeout);
            _activeThreads.Remove(thread);
            _suspendedThreads.Add(thread);
        }

        public void SuspendThread(uint threadId, TimeSpan timeout)
        {
            if (threadId >= _threads.Count)
            {
                throw new ArgumentOutOfRangeException($"There is no thread with ID {threadId}.");
            }

            var thread = _threads[(int)threadId];
            SuspendThread(thread, timeout);
        }

        public void ResumeThread(ThreadContext thread)
        {
            thread.Resume();
            _activeThreads.Add(thread);
            _suspendedThreads.Remove(thread);
        }

        public void ResumeThread(uint threadId)
        {
            if (threadId >= _threads.Count)
            {
                throw new ArgumentOutOfRangeException($"There is no thread with ID {threadId}.");
            }

            var thread = _threads[(int)threadId];
            ResumeThread(thread);
        }  
    }
}
