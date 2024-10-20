namespace BotDeScans.App.Models;

public record ErrorInfo(string Message, int Number, int Depth, ErrorType Type);

public enum ErrorType
{
    Regular,
    Exception
}
