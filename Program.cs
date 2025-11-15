using AuthenticationConsoleSystem;

namespace AuthenticationConsoleSystem
{
    class Program
    {
        private static readonly MigrationService _migrationService =
            new(DatabaseConfig.ConnectionFactory);

        static async Task Main(string[] args)
        {
            try
            {
                await ShowWelcomeScreenAsync();
                await InitializeDatabaseAsync();
                await RunCommandLoopAsync();
            }
            catch (Exception ex)
            {
                ConsolePersonalizer.ColorPrint($"Fatal error: {ex.Message}", ConsoleColor.Red);
                ConsolePersonalizer.ColorPrint("The application will now close.", ConsoleColor.Red);
                Environment.Exit(1);
            }
        }

        private static async Task ShowWelcomeScreenAsync()
        {
            Console.Clear();
            ConsolePersonalizer.ColorPrint(@"
  █████╗ ██╗   ██╗████████╗██╗  ██╗███████╗███╗   ██╗████████╗██╗ ██████╗ █████╗ ████████╗██╗ ██████╗ ███╗   ██╗
 ██╔══██╗██║   ██║╚══██╔══╝██║  ██║██╔════╝████╗  ██║╚══██╔══╝██║██╔════╝██╔══██╗╚══██╔══╝██║██╔═══██╗████╗  ██║
 ███████║██║   ██║   ██║   ███████║█████╗  ██╔██╗ ██║   ██║   ██║██║     ███████║   ██║   ██║██║   ██║██╔██╗ ██║
 ██╔══██║██║   ██║   ██║   ██╔══██║██╔══╝  ██║╚██╗██║   ██║   ██║██║     ██╔══██║   ██║   ██║██║   ██║██║╚██╗██║
 ██║  ██║╚██████╔╝   ██║   ██║  ██║███████╗██║ ╚████║   ██║   ██║╚██████╗██║  ██║   ██║   ██║╚██████╔╝██║ ╚████║
 ╚═╝  ╚═╝ ╚═════╝    ╚═╝   ╚═╝  ╚═╝╚══════╝╚═╝  ╚═══╝   ╚═╝   ╚═╝ ╚═════╝╚═╝  ╚═╝   ╚═╝   ╚═╝ ╚═════╝ ╚═╝  ╚═══╝
                                                                                                                   
 ┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
 │                                 Welcome to Authentication Console System!                                   │
 │                           Type 'help' for available commands or 'exit' to quit                              │
 └─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘",
 ConsoleColor.Cyan);

            // Mostrar información del sistema
            ConsolePersonalizer.ColorPrint($"\n {DateTime.Now:dddd, MMMM dd, yyyy} | {DateTime.Now:HH:mm:ss}", ConsoleColor.Gray);
            ConsolePersonalizer.ColorPrint("Secure Authentication System with PostgreSQL\n", ConsoleColor.Gray);
        }

        private static async Task InitializeDatabaseAsync()
        {
            ConsolePersonalizer.ColorPrint("\nChecking database status...", ConsoleColor.Yellow);

            await _migrationService.CheckDatabaseStatusAsync();

            ConsolePersonalizer.ColorPrint("\nDo you want to run database migrations? (y/n): ", ConsoleColor.Yellow);
            var response = Console.ReadLine()?.Trim().ToLower();

            if (response == "y" || response == "yes")
            {
                try
                {
                    await _migrationService.MigrateAsync();
                }
                catch (Exception ex)
                {
                    ConsolePersonalizer.ColorPrint($"Migration failed: {ex.Message}", ConsoleColor.Red);
                    ConsolePersonalizer.ColorPrint("You can try running 'migrate' command later or check your database connection.", ConsoleColor.Yellow);
                }
            }
            else
            {
                ConsolePersonalizer.ColorPrint("Skipping migrations. You can run 'migrate' command later if needed.", ConsoleColor.Yellow);
            }

            Console.WriteLine();
        }

        private static async Task RunCommandLoopAsync()
        {
            var showHint = true;

            while (true)
            {
                try
                {
                    if (showHint)
                    {
                        ConsolePersonalizer.ColorPrint("Tip: Type 'help' to see all available commands", ConsoleColor.DarkGray);
                        showHint = false;
                    }

                    Console.Write("> ");

                    string input = (Console.ReadLine() ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        continue;
                    }

                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("q", StringComparison.OrdinalIgnoreCase))
                    {
                        await ExitApplicationAsync();
                        break;
                    }

                    if (input.Equals("clear", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("cls", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.Clear();
                        continue;
                    }

                    await CommandProcesser.ProcessCommandAsync(input);
                    Console.WriteLine(); // Línea en blanco entre comandos
                }
                catch (Exception ex)
                {
                    ConsolePersonalizer.ColorPrint($"Command error: {ex.Message}", ConsoleColor.Red);
                    ConsolePersonalizer.ColorPrint("Type 'help' for available commands or 'exit' to quit.", ConsoleColor.Yellow);
                }
            }
        }

        private static async Task ExitApplicationAsync()
        {
            ConsolePersonalizer.ColorPrint("\n┌───────────────────────────────────────────────────────┐", ConsoleColor.Cyan);
            ConsolePersonalizer.ColorPrint("│                 Session Summary                       │", ConsoleColor.Cyan);
            ConsolePersonalizer.ColorPrint("├───────────────────────────────────────────────────────┤", ConsoleColor.Cyan);
            ConsolePersonalizer.ColorPrint($"│  Session ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}                   │", ConsoleColor.White);
            ConsolePersonalizer.ColorPrint("│  Thank you for using Authentication Console System!   │", ConsoleColor.White);
            ConsolePersonalizer.ColorPrint("└───────────────────────────────────────────────────────┘", ConsoleColor.Cyan);

            // Pequeña animación de despedida
            for (int i = 0; i < 3; i++)
            {
                ConsolePersonalizer.ColorPrint("   Shutting down" + new string('.', i + 1), ConsoleColor.Gray);
                await Task.Delay(300);
            }

            ConsolePersonalizer.ColorPrint("\nGoodbye! Come back soon!\n", ConsoleColor.Green);
            await Task.Delay(500);
        }
    }
}