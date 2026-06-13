using System.Text;

namespace WsdlGenerator.Generator;

internal sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    public void Indent() => _indent++;
    public void Dedent() => _indent = Math.Max(0, _indent - 1);

    public void Line(string text = "")
    {
        if (text.Length == 0)
            _sb.AppendLine();
        else
            _sb.Append(' ', _indent * 4).AppendLine(text);
    }

    public void OpenBrace()
    {
        Line("{");
        Indent();
    }

    public void CloseBrace(string suffix = "")
    {
        Dedent();
        Line("}" + suffix);
    }

    public override string ToString() => _sb.ToString();
}
