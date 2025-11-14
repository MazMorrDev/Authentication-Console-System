using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthenticationConsoleSystem;

public class CommandProcesser
{
    // private static UsersService _usersService = new UsersService();
    private static readonly User _currentUser = new(); // Para trackear el usuario logueado
    private static readonly Func<DbConnection> _connectionFactory = DatabaseConfig.ConnectionFactory;
    private static readonly UserService _userService = new(_connectionFactory);
    private static readonly RoleService _roleService = new(_connectionFactory);
    private static readonly UserRoleService _userRoleService = new(_connectionFactory);

    public static void ProcessCommand(string input)
    {
        // Dividir el input por espacios para manejar comandos con parámetros
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();

        switch (command)
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
            case "logout":
                            if (parts.Length > 1)
                {
                    ExecuteLogout(ParseIntWithErrorHandler(parts[1]));
                }
                break;
            case "info":
                if (parts.Length > 1)
                {
                    ExecuteInfo(ParseIntWithErrorHandler(parts[1]));
                }

                break;
            case "exit":
                ExecuteExit();
                break;
            default:
                ConsolePersonalizer.ColorPrint(
                    $"I can't do nothing with '{input}', please type 'help'",
                    ConsoleColor.Red);
                break;
        }
    }

    private static int? ParseIntWithErrorHandler(string number)
    {
        try
        {
            int Id = int.Parse(number);
            return Id;
        }
        catch (Exception e)
        {
            ConsolePersonalizer.ColorPrint($"An error has been ocurred: {e.Message}", ConsoleColor.Green);

        }
        return null;
    }

    private static void ExecuteLogout(int? id)
    {
        if (_currentUser == null)
        {
            ConsolePersonalizer.ColorPrint("No user is currently logged in.", ConsoleColor.Yellow);
            return;
        }

        _currentUser.IsLogged = false;
        //Acordarse de guardarlo en la base de datos
        ConsolePersonalizer.ColorPrint("Successfully logged out!", ConsoleColor.Green);
    }

    private static void ExecuteInfo(int? id)
    {
        if (id == null)
        {
            ConsolePersonalizer.ColorPrint("You didn't pass the ID from the user that you are looking for", ConsoleColor.Red);
            return;
        }
        else if (id != null)
        {
            // Buscar información de usuario por ID
            User user = _userService.GetByIdAsync((int)id).Result ?? new User();

            if (user != null)
            {
                DisplayUserInfo(user);
            }
            else
            {
                ConsolePersonalizer.ColorPrint($"User with ID '{id}' not found.", ConsoleColor.Red);
            }
        }

    }

    private static void DisplayUserInfo(User user)
    {
        ConsolePersonalizer.ColorPrint($"User Information:", ConsoleColor.Cyan);
        ConsolePersonalizer.ColorPrint($"ID: {user.Id}", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint($"Username: {user.UserName}", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint($"Is Logged: {user.IsLogged}", ConsoleColor.White);
    }

    private static void ExecuteLogin()
    {
        if (_currentUser != null)
        {
            ConsolePersonalizer.ColorPrint($"You are already logged in as {_currentUser.UserName}. Please logout first.", ConsoleColor.Yellow);
            return;
        }

        ConsolePersonalizer.ColorPrint("Please write your username:", ConsoleColor.Green);
        string username = Console.ReadLine() ?? "";

        ConsolePersonalizer.ColorPrint("Please write your password:", ConsoleColor.Green);
        string password = Console.ReadLine() ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ConsolePersonalizer.ColorPrint("Username and password cannot be empty.", ConsoleColor.Red);
            return;
        }

        // // Llamar al servicio para login
        // var loginResult = _usersService.Login(username, password);

        // if (loginResult.Success)
        // {
        //     _currentUser = loginResult.User;
        //     ConsolePersonalizer.ColorPrint($"Welcome back, {_currentUser.Username}!", ConsoleColor.Green);
        // }
        // else
        // {
        //     ConsolePersonalizer.ColorPrint(loginResult.Message, ConsoleColor.Red);
        // }
    }

    private static void ExecuteRegisterUser()
    {
        ConsolePersonalizer.ColorPrint("Please write your username:", ConsoleColor.Green);
        string username = Console.ReadLine() ?? "";

        ConsolePersonalizer.ColorPrint("Please write your password:", ConsoleColor.Green);
        string password = Console.ReadLine() ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ConsolePersonalizer.ColorPrint("Username and password cannot be empty.", ConsoleColor.Red);
            return;
        }

        if (username.Length < 3)
        {
            ConsolePersonalizer.ColorPrint("Username must be at least 3 characters long.", ConsoleColor.Red);
            return;
        }

        if (password.Length < 6)
        {
            ConsolePersonalizer.ColorPrint("Password must be at least 6 characters long.", ConsoleColor.Red);
            return;
        }

        // // Crear y registrar el usuario
        // var registerResult = _usersService.RegisterUser(username, password);

        // if (registerResult.Success)
        // {
        //     ConsolePersonalizer.ColorPrint($"User '{username}' registered successfully!", ConsoleColor.Green);
        // }
        // else
        // {
        //     ConsolePersonalizer.ColorPrint(registerResult.Message, ConsoleColor.Red);
        // }
    }

    private static void NullConsoleReadLineHandler(string readedLine, string attribute)
    {
        if (string.IsNullOrEmpty(readedLine))
        {
            ConsolePersonalizer.ColorPrint($"Please ensure to write your {attribute}", ConsoleColor.DarkRed);
        }
    }

    private static void ExecuteHelp()
    {
        ConsolePersonalizer.ColorPrint(
            "AuthenticationConsoleSystem \n\n" +
            "register     Create a new user account\n" +
            "login        Login with an existent user account\n" +
            "logout       Logout an existent user account\n" +
            "info         Shows information about the current logged in user\n" +
            "info <id>    Shows information about the user with the provided Id\n" +
            "exit         Shutdown the application",
            ConsoleColor.DarkCyan);
    }

    private static void ExecuteExit()
    {
        ConsolePersonalizer.ColorPrint("Goodbye!", ConsoleColor.Cyan);
        Environment.Exit(0);
    }
}