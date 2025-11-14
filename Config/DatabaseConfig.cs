using System.Data.Common;
using Npgsql;

namespace AuthenticationConsoleSystem;

public static class DatabaseConfig
{
    // Cadena de conexión CORRECTA para PostgreSQL
    public static string ConnectionString { get; } =
        "Host=localhost;Port=5432;Database=authentication_console_system;Username=mazmorrdev;Password=KevinICC12;";

    public static Func<DbConnection> ConnectionFactory => () =>
    {
        var connection = new NpgsqlConnection(ConnectionString);
        ConsolePersonalizer.ColorPrint("Connection sucessfully established", ConsoleColor.Green);
        return connection;
    };
}

// // How To Use DatabaseConfig:
//    private static readonly Func<DbConnection> _connectionFactory = DatabaseConfig.ConnectionFactory;
//    private static readonly UserService _userService = new(_connectionFactory);
