using System.Data.Common;

namespace AuthenticationConsoleSystem;

public class CommandProcesser
{
    private static readonly Func<DbConnection> _connectionFactory = DatabaseConfig.ConnectionFactory;
    private static readonly UserService _userService = new(_connectionFactory);

    public static async Task ProcessCommandAsync(string input)
    {
        // Dividir el input por espacios para manejar comandos con parámetros
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();

        switch (command)
        {
            case "help":
                ExecuteHelp();
                break;
            case "list":
                await ExecuteListAsync();
                break;
            case "register":
                await ExecuteRegisterUserAsync();
                break;
            case "login":
                await ExecuteLoginAsync();
                break;
            case "logout":
                if (parts.Length > 1)
                    await ExecuteLogoutAsync(ParseHelper.ParseIntWithErrorHandler(parts[1]));
                else
                    ConsolePersonalizer.ColorPrint("Please provide user ID: logout <id>", ConsoleColor.Red);
                break;
            case "info":
                if (parts.Length > 1)
                    await ExecuteInfoAsync(ParseHelper.ParseIntWithErrorHandler(parts[1]));
                else
                    ConsolePersonalizer.ColorPrint("Please provide user ID: info <id>", ConsoleColor.Red);
                break;
            case "migrate":
                await ExecuteMigrateAsync();
                break;
            case "db-status":
                await ExecuteDbStatusAsync();
                break;
            default:
                ConsolePersonalizer.ColorPrint(
                    $"I can't do nothing with '{input}', please type 'help'",
                    ConsoleColor.Red);
                break;
        }
    }

    private static async Task ExecuteMigrateAsync()
    {
        try
        {
            var migrationService = new MigrationService(_connectionFactory);
            await migrationService.MigrateAsync();
        }
        catch (Exception ex)
        {
            ConsolePersonalizer.ColorPrint($"Migration failed: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static async Task ExecuteDbStatusAsync()
    {
        var migrationService = new MigrationService(_connectionFactory);
        await migrationService.CheckDatabaseStatusAsync();
    }

    private static async Task ExecuteListAsync()
    {
        try
        {
            var users = await _userService.GetAllAsync();
            if (users.Count < 1)
            {
                ConsolePersonalizer.ColorPrint("There aren't accounts to show", ConsoleColor.DarkRed);
                return;
            }

            ConsolePersonalizer.ColorPrint($"Found {users.Count} user(s):", ConsoleColor.DarkBlue);
            foreach (var user in users)
            {
                var status = user.IsLogged ? "🟢 ONLINE" : "🔴 OFFLINE";
                ConsolePersonalizer.ColorPrint($"ID: {user.Id} | User: {user.UserName} | {status}",
                    user.IsLogged ? ConsoleColor.Green : ConsoleColor.DarkGray);
            }
        }
        catch (Exception ex)
        {
            ConsolePersonalizer.ColorPrint($"Error retrieving users: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static async Task ExecuteLogoutAsync(int? id)
    {
        if (id == null)
        {
            ConsolePersonalizer.ColorPrint("Invalid user ID", ConsoleColor.Red);
            return;
        }

        try
        {
            var result = await _userService.LogoutUserAsync(id.Value);
            if (result)
            {
                ConsolePersonalizer.ColorPrint($"User with ID {id} logged out successfully!", ConsoleColor.Green);
            }
            else
            {
                ConsolePersonalizer.ColorPrint($"User with ID {id} not found or already logged out", ConsoleColor.Yellow);
            }
        }
        catch (Exception ex)
        {
            ConsolePersonalizer.ColorPrint($"Error during logout: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static async Task ExecuteInfoAsync(int? id)
    {
        if (id == null)
        {
            ConsolePersonalizer.ColorPrint("You didn't provide a valid user ID", ConsoleColor.Red);
            return;
        }

        try
        {
            var user = await _userService.GetByIdAsync(id.Value);
            if (user != null)
            {
                DisplayUserInfo(user);
            }
            else
            {
                ConsolePersonalizer.ColorPrint($"User with ID '{id}' not found.", ConsoleColor.Red);
            }
        }
        catch (Exception ex)
        {
            ConsolePersonalizer.ColorPrint($"Error retrieving user info: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static void DisplayUserInfo(User user)
    {
        ConsolePersonalizer.ColorPrint("╔══════════════════════════════════╗", ConsoleColor.Cyan);
        ConsolePersonalizer.ColorPrint("║         USER INFORMATION         ║", ConsoleColor.Cyan);
        ConsolePersonalizer.ColorPrint("╠══════════════════════════════════╣", ConsoleColor.Cyan);
        ConsolePersonalizer.ColorPrint($"║ ID: {user.Id,-28} ║", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint($"║ Username: {user.UserName,-20} ║", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint($"║ Status: {(user.IsLogged ? "LOGGED IN" : "LOGGED OUT"),-21} ║",
            user.IsLogged ? ConsoleColor.Green : ConsoleColor.Yellow);
        ConsolePersonalizer.ColorPrint("║                                  ║", ConsoleColor.Cyan);
        ConsolePersonalizer.ColorPrint($"║ Password Hash:                  ║", ConsoleColor.White);

        if (!string.IsNullOrEmpty(user.HashPassword))
        {
            var hashPreview = user.HashPassword.Length > 20
                ? user.HashPassword[..20] + "..."
                : user.HashPassword;
            ConsolePersonalizer.ColorPrint($"║ {hashPreview,-32} ║", ConsoleColor.DarkGray);
        }

        ConsolePersonalizer.ColorPrint("╚══════════════════════════════════╝", ConsoleColor.Cyan);
    }

    private static async Task ExecuteLoginAsync()
    {
        ConsolePersonalizer.ColorPrint("Please enter your username:", ConsoleColor.Green);
        string username = Console.ReadLine()?.Trim() ?? "";

        ConsolePersonalizer.ColorPrint("Please enter your password:", ConsoleColor.Green);
        string password = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ConsolePersonalizer.ColorPrint("Username and password cannot be empty.", ConsoleColor.Red);
            return;
        }

        try
        {
            var loginResult = await _userService.LoginUserAsync(username, password);

            if (loginResult != null)
            {
                ConsolePersonalizer.ColorPrint($"🎉 Welcome back, {loginResult.UserName}!", ConsoleColor.Green);
                ConsolePersonalizer.ColorPrint($"Your user ID is: {loginResult.Id}", ConsoleColor.Cyan);
            }
            else
            {
                ConsolePersonalizer.ColorPrint("❌ Invalid username or password", ConsoleColor.Red);
            }
        }
        catch (Exception ex)
        {
            ConsolePersonalizer.ColorPrint($"Error during login: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static async Task ExecuteRegisterUserAsync()
    {
        ConsolePersonalizer.ColorPrint("Please enter your username:", ConsoleColor.Green);
        string username = Console.ReadLine()?.Trim() ?? "";

        ConsolePersonalizer.ColorPrint("Please enter your password:", ConsoleColor.Green);
        string password = Console.ReadLine()?.Trim() ?? "";

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

        if (password.Length < 4)
        {
            ConsolePersonalizer.ColorPrint("Password must be at least 4 characters long.", ConsoleColor.Red);
            return;
        }

        try
        {
            var registerResult = await _userService.RegisterUserAsync(username, password);

            if (registerResult)
            {
                ConsolePersonalizer.ColorPrint($"✅ User '{username}' registered successfully!", ConsoleColor.Green);
                ConsolePersonalizer.ColorPrint("You can now login with your credentials", ConsoleColor.Cyan);
            }
            else
            {
                ConsolePersonalizer.ColorPrint("❌ Registration failed. Username might already exist.", ConsoleColor.Red);
            }
        }
        catch (Exception ex)
        {
            ConsolePersonalizer.ColorPrint($"Error during registration: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static void ExecuteHelp()
    {
        ConsolePersonalizer.ColorPrint("╔════════════════════════════════════════════════════╗", ConsoleColor.DarkCyan);
        ConsolePersonalizer.ColorPrint("║           AUTHENTICATION CONSOLE SYSTEM            ║", ConsoleColor.DarkCyan);
        ConsolePersonalizer.ColorPrint("╠════════════════════════════════════════════════════╣", ConsoleColor.DarkCyan);
        ConsolePersonalizer.ColorPrint("║ Available commands:                                ║", ConsoleColor.DarkCyan);
        ConsolePersonalizer.ColorPrint("║                                                    ║", ConsoleColor.DarkCyan);
        ConsolePersonalizer.ColorPrint("║ list          - Show all user accounts             ║", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint("║ register      - Create a new user account          ║", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint("║ login         - Login with existing account        ║", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint("║ logout <id>   - Logout user with specified ID      ║", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint("║ info <id>     - Show user information by ID        ║", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint("║ migrate       - Run database migrations            ║", ConsoleColor.Yellow);
        ConsolePersonalizer.ColorPrint("║ db-status     - Check database status              ║", ConsoleColor.Yellow);
        ConsolePersonalizer.ColorPrint("║ exit          - Shutdown the application           ║", ConsoleColor.White);
        ConsolePersonalizer.ColorPrint("║                                                    ║", ConsoleColor.DarkCyan);
        ConsolePersonalizer.ColorPrint("╚════════════════════════════════════════════════════╝", ConsoleColor.DarkCyan);
    }
}