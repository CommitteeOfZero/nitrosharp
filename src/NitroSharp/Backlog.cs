using NitroSharp.Text;
using NitroSharp.Utilities;
using System;
using System.Text;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;

#nullable enable

namespace NitroSharp
{
    internal readonly struct BacklogEntry
    {
        public readonly string Text;

        public BacklogEntry(string text)
        {
            Text = text;
        }
    }

    internal sealed class Backlog
    {
        private readonly SystemVariableLookup _systemVariables;
        private ArrayBuilder<BacklogEntry> _entries;
        private readonly StringBuilder _sb;

        public Backlog(SystemVariableLookup systemVariables)
        {
            _systemVariables = systemVariables;
            _entries = new ArrayBuilder<BacklogEntry>(1024);
            _sb = new StringBuilder();
        }

        public void Append(TextSegment text)
        {
            _sb.Clear();
            foreach (TextRun textRun in text.TextRuns)
            {
                if (!textRun.HasRubyText)
                {
                    _sb.Append(textRun.Text);
                }
            }

            if (_sb.Length > 0)
            {
                string s = _sb.ToString();
                _systemVariables.LastText = ConstantValue.String(s);
                _entries.Add(new BacklogEntry(s));
            }
        }

        public ReadOnlySpan<BacklogEntry> Entries => _entries.AsReadonlySpan();

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
