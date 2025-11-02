
namespace AuthenticationConsoleSystem;

public class CommandProcesser
{

    public static void ProcessCommand(string input)
    {
        switch (input)
        {
            case "help":
                ExecuteHelp();
                break;
            case "register":
                ExecuteRegisterUser();
                break;
            case "login":
                ExecuteLogin();
                break;
            default:
                ConsolePersonalizer.ColoredPrint($"I can't do nothing with {input}, please type 'help' ", ConsoleColor.Red);
                break;
        }
    }

    private static void ExecuteLogin()
    {
        throw new NotImplementedException();
    }

    private static void ExecuteRegisterUser()
    {
        throw new NotImplementedException();
    }

    private static void ExecuteHelp()
    {
        throw new NotImplementedException();
    }
}
