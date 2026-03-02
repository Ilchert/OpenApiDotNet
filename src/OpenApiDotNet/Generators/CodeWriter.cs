using System.Text;

namespace OpenApiDotNet.Generators;

internal class CodeWriter
{
    private readonly StringBuilder _builder = new();
    private int _indentLevel = 0;
    private const int IndentLength = 4;
    public void Write(string text)
    {
        if (_builder.Length == 0 || _builder[_builder.Length - 1] == '\n')
            _builder.Append(new string(' ', _indentLevel * IndentLength));
        _builder.Append(text);
    }
    public void WriteLine(string text = "")
    {
        Write(text + Environment.NewLine);
    }
    public void Indent() => _indentLevel++;
    public void Unindent() => _indentLevel--;
    public override string ToString() => _builder.ToString();
}
