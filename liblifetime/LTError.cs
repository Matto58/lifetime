namespace Mattodev.Lifetime;

public class LTError(string message, string file, string lineContent, int lineNumber)
{
    public string Message { get; set; } = message;
    public string File { get; set; } = file;
    public (string Content, int Number) Line { get; set; } = (lineContent, lineNumber);
}
