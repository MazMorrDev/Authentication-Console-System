namespace AuthenticationConsoleSystem;

public class ConsolePersonalizer
{
    public static void ColoredPrint(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
        Console.WriteLine("");
    }
}
