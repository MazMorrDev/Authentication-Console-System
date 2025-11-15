using AuthenticationConsoleSystem;

ConsolePersonalizer.ColorPrint(@"
  __  __           __  __                 ____             
 |  \/  | __ _ ___|  \/  | ___  _ __ _ __|  _ \  _____   __
 | |\/| |/ _` |_  / |\/| |/ _ \| '__| '__| | | |/ _ \ \ / /
 | |  | | (_| |/ /| |  | | (_) | |  | |  | |_| |  __/\ V / 
 |_|  |_|\__,_/___|_|  |_|\___/|_|  |_|  |____/ \___| \_/  
                                                           
┌──────────────────────────────────────────────────────────┐
│   Welcome to Authentication Console System! Type 'help'  │
│   for instructions or 'order' to start processing files  │
└──────────────────────────────────────────────────────────┘",
 ConsoleColor.Yellow);


while (true)
{
    Console.Write("▸ ");
    string input = (Console.ReadLine() ?? "").Trim().ToLower();
    if (input.Equals("exit"))
    {
        ConsolePersonalizer.ColorPrint("👋 Goodbye! Come back soon! :D", ConsoleColor.Cyan);
        Environment.Exit(0);
    }
    await CommandProcesser.ProcessCommandAsync(input);
}