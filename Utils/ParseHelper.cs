namespace AuthenticationConsoleSystem;

public class ParseHelper
{

    public static int? ParseIntWithErrorHandler(string text)
    {
        try
        {
            int number = int.Parse(text);
            return number;
        }
        catch (Exception e)
        {
            ConsolePersonalizer.ColorPrint($"There was a problem while parsing: {e}", ConsoleColor.DarkRed);
            return null;
        }

    }
}
