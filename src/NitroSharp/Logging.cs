using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NitroSharp.Utilities;

namespace NitroSharp
{
    internal enum LogLevel
    {
        Information,
        Warning,
        Error
    }

    [Flags]
    internal enum LogEventFlags
    {
        None = 1 << 0,
        MissingFeature = 1 << 1,
        MissingAsset = 1 << 2
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct LogEvent
    {
        public readonly string Message;
        public readonly DateTimeOffset Timestamp;
        public readonly LogLevel LogLevel;
        public readonly LogEventFlags Flags;

        public LogEvent(
            string message,
            DateTimeOffset timestamp,
            LogLevel logLevel,
            LogEventFlags flags)
        {
            Message = message;
            Timestamp = timestamp;
            LogLevel = logLevel;
            Flags = flags;
        }
    }

    internal sealed class LoggerConfiguration
    {
        private readonly ImmutableArray<ILogEventSink>.Builder _eventSinks;

        public LoggerConfiguration()
        {
            _eventSinks = ImmutableArray.CreateBuilder<ILogEventSink>();
        }

        public LoggerConfiguration WithSink(ILogEventSink sink)
        {
            _eventSinks.Add(sink);
            return this;
        }

        public Logger CreateLogger()
            => new(_eventSinks.ToImmutableArray());
    }

    internal sealed class Logger
    {
        private readonly LogLevel _minimumLevel;
        private ImmutableArray<ILogEventSink> _eventSinks;

        public Logger(ImmutableArray<ILogEventSink> eventSinks)
        {
            _eventSinks = eventSinks;
        }

        public void LogError(string message, LogEventFlags flags = LogEventFlags.None)
            => Log(message, LogLevel.Error, flags);

        public void LogInformation(string message, LogEventFlags flags = LogEventFlags.None)
            => Log(message, LogLevel.Information, flags);

        public void LogWarning(string message, LogEventFlags flags = LogEventFlags.None)
            => Log(message, LogLevel.Warning, flags);

        public void LogError(StringBuilder message, LogEventFlags flags = LogEventFlags.None)
            => Log(message, LogLevel.Error, flags);

        public void LogInformation(StringBuilder message, LogEventFlags flags = LogEventFlags.None)
            => Log(message, LogLevel.Information, flags);

        public void LogWarning(StringBuilder message, LogEventFlags flags = LogEventFlags.None)
            => Log(message, LogLevel.Warning, flags);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Log(string message, LogLevel logLevel, LogEventFlags flags)
        {
            if ((int)logLevel >= (int)_minimumLevel)
            {
                Write(message, logLevel, flags);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Log(StringBuilder message, LogLevel logLevel, LogEventFlags flags)
        {
            if ((int)logLevel >= (int)_minimumLevel)
            {
                Write(message.ToString(), logLevel, flags);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Write(string message, LogLevel logLevel, LogEventFlags flags)
        {
            var logEvent = new LogEvent(message, DateTimeOffset.Now, logLevel, flags);
            foreach (ILogEventSink sink in _eventSinks)
            {
                sink.Emit(logEvent);
            }
        }
    }

    internal interface ILogEventSink
    {
        void Emit(in LogEvent logEvent);
    }

    internal abstract class LogEventSink
    {
    }

    internal sealed class ConsoleLogEventSink : ILogEventSink
    {
        public void Emit(in LogEvent logEvent)
        {
            Console.Write("[");
            Console.Write(logEvent.Timestamp.ToString());

            Console.ForegroundColor = logEvent.LogLevel switch
            {
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                _ => ThrowHelper.Unreachable<ConsoleColor>()
            };

            Console.Write(logEvent.LogLevel switch
            {
                LogLevel.Information => " INF",
                LogLevel.Warning => " WRN",
                LogLevel.Error => " ERR",
                _ => ThrowHelper.Unreachable<string>()
            });

            Console.ResetColor();
            Console.Write("] ");
            Console.Write(logEvent.Message);
            Console.WriteLine();
        }
    }

    internal sealed class LogEventRecorder : ILogEventSink
    {
        private ArrayBuilder<LogEvent> _logEvents = new(16);
        private int _count;
        private readonly object _lock = new();

        public ReadOnlySpan<LogEvent> LogEvents =>
            _logEvents.AsReadonlySpan(0, _count);

        public void Emit(in LogEvent logEvent)
        {
            lock (_lock)
            {
                _logEvents.Add() = logEvent;
                _count++;
            }
        }
    }
}
