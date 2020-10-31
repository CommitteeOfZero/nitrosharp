using NitroSharp.Text;
using NitroSharp.Utilities;
using System;
using System.Text;

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
        private ArrayBuilder<BacklogEntry> _entries;
        private readonly StringBuilder _sb;

        public Backlog()
        {
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
                _entries.Add(new BacklogEntry(_sb.ToString()));
            }
        }

        public ReadOnlySpan<BacklogEntry> Entries => _entries.AsReadonlySpan();

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
