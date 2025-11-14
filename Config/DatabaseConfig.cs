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

// Como usar DatabaseConfig:
// // En Program.cs
// var connectionString = "Data Source=authentication.db";
// Func<DbConnection> connectionFactory = () => new SqliteConnection(connectionString);

// // Instanciar servicios
// var usersService = new UsersService(connectionFactory);
// var rolesService = new RolesServices(connectionFactory);
// var userRolesService = new UserRolesService(connectionFactory);