using System.Text;

namespace SharpOnvif.CodeGen.Emit;

/// <summary>A small indenting text writer for emitting C# source.</summary>
internal sealed class CSharpWriter
{
    private readonly StringBuilder _buffer = new();
    private int _indent;
    private bool _atLineStart = true;

    public void Indent() => _indent++;

    public void Outdent() => _indent--;

    /// <summary>Writes an open brace, indents, and returns a scope that closes it.</summary>
    public Block Braces()
    {
        Line("{");
        Indent();
        return new Block(this);
    }

    public void Line(string text = "")
    {
        if (text.Length == 0)
        {
            _buffer.Append('\n');
            _atLineStart = true;
            return;
        }

        WriteIndent();
        _buffer.Append(text).Append('\n');
        _atLineStart = true;
    }

    /// <summary>Writes each line of a multi-line string at the current indent.</summary>
    public void Lines(string text)
    {
        foreach (var line in text.Split('\n'))
            Line(line.TrimEnd('\r'));
    }

    public void Write(string text)
    {
        WriteIndent();
        _buffer.Append(text);
    }

    private void WriteIndent()
    {
        if (!_atLineStart) return;
        _buffer.Append(' ', _indent * 4);
        _atLineStart = false;
    }

    /// <summary>
    /// Emits a documentation comment, escaping XML and wrapping long schema documentation so the
    /// generated files stay readable.
    /// </summary>
    public void Doc(string? documentation)
    {
        if (string.IsNullOrWhiteSpace(documentation)) return;

        Line("/// <summary>");
        foreach (var line in Wrap(Escape(documentation!), 100))
            Line("/// " + line);
        Line("/// </summary>");
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static IEnumerable<string> Wrap(string text, int width)
    {
        var line = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return line.ToString();
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0) yield return line.ToString();
    }

    public override string ToString() => _buffer.ToString();

    internal readonly struct Block : IDisposable
    {
        private readonly CSharpWriter _writer;
        public Block(CSharpWriter writer) => _writer = writer;

        public void Dispose()
        {
            _writer.Outdent();
            _writer.Line("}");
        }
    }
}
