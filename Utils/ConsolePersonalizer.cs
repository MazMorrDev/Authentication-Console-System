namespace AuthenticationConsoleSystem;

public class ConsolePersonalizer
{
    public static void ColorPrint(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}
