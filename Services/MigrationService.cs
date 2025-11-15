using System.Data.Common;
using Npgsql;

namespace AuthenticationConsoleSystem;

public class MigrationService
{
    private readonly Func<DbConnection> _connectionFactory;

    public MigrationService(Func<DbConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // Método principal para ejecutar todas las migraciones
    public async Task MigrateAsync()
    {
        ConsolePersonalizer.ColorPrint("Starting database migrations...", ConsoleColor.Yellow);
        
        // Crear la tabla de control de migraciones primero
        await CreateMigrationsTableAsync();
        
        // Ejecutar migraciones en orden
        await ExecuteMigrationAsync("001_CreateUsersTable", CreateUsersTableAsync);
        await ExecuteMigrationAsync("002_CreateUserRolesTable", CreateUserRolesTableAsync);
        await ExecuteMigrationAsync("003_InsertDefaultRoles", InsertDefaultRolesAsync);
        
        ConsolePersonalizer.ColorPrint("Database migrations completed successfully!", ConsoleColor.Green);
    }

    // Crear tabla para control de migraciones
    private async Task CreateMigrationsTableAsync()
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS ""__Migrations"" (
                ""MigrationId"" VARCHAR(150) PRIMARY KEY,
                ""AppliedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
            );";

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    // Verificar si una migración ya fue aplicada
    private async Task<bool> IsMigrationAppliedAsync(string migrationId)
    {
        const string sql = @"SELECT COUNT(1) FROM ""__Migrations"" WHERE ""MigrationId"" = @MigrationId;";
        
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        
        var param = cmd.CreateParameter();
        param.ParameterName = "@MigrationId";
        param.Value = migrationId;
        cmd.Parameters.Add(param);
        
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    // Marcar migración como aplicada
    private async Task MarkMigrationAsAppliedAsync(string migrationId)
    {
        const string sql = @"INSERT INTO ""__Migrations"" (""MigrationId"") VALUES (@MigrationId);";
        
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        
        var param = cmd.CreateParameter();
        param.ParameterName = "@MigrationId";
        param.Value = migrationId;
        cmd.Parameters.Add(param);
        
        await cmd.ExecuteNonQueryAsync();
    }

    // Ejecutar una migración específica si no ha sido aplicada
    private async Task ExecuteMigrationAsync(string migrationId, Func<Task> migrationAction)
    {
        if (await IsMigrationAppliedAsync(migrationId))
        {
            ConsolePersonalizer.ColorPrint($"✓ Migration {migrationId} already applied", ConsoleColor.DarkGray);
            return;
        }

        ConsolePersonalizer.ColorPrint($"Applying migration {migrationId}...", ConsoleColor.Cyan);
        
        try
        {
            await migrationAction();
            await MarkMigrationAsAppliedAsync(migrationId);
            ConsolePersonalizer.ColorPrint($"✓ Migration {migrationId} applied successfully", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            ConsolePersonalizer.ColorPrint($"✗ Migration {migrationId} failed: {ex.Message}", ConsoleColor.Red);
            throw;
        }
    }

    // Migración 1: Crear tabla Users
    private async Task CreateUsersTableAsync()
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""UserName"" VARCHAR(100) NOT NULL UNIQUE,
                ""HashPassword"" VARCHAR(255) NOT NULL,
                ""IsLogged"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
            );

            -- Crear índice para búsquedas por nombre de usuario
            CREATE INDEX IF NOT EXISTS ""IX_Users_UserName"" ON ""Users"" (""UserName"");

            -- Crear trigger para actualizar UpdatedAt automáticamente
            CREATE OR REPLACE FUNCTION update_updated_at_column()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.""UpdatedAt"" = NOW();
                RETURN NEW;
            END;
            $$ language 'plpgsql';

            DROP TRIGGER IF EXISTS ""trg_update_users_updatedat"" ON ""Users"";
            CREATE TRIGGER ""trg_update_users_updatedat""
                BEFORE UPDATE ON ""Users""
                FOR EACH ROW
                EXECUTE FUNCTION update_updated_at_column();";

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    // Migración 2: Crear tabla UserRoles (si necesitas sistema de roles)
    private async Task CreateUserRolesTableAsync()
    {
        const string sql = @"
            -- Tabla de Roles
            CREATE TABLE IF NOT EXISTS ""Roles"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Name"" VARCHAR(50) NOT NULL UNIQUE,
                ""Description"" TEXT
            );

            -- Tabla intermedia UserRoles
            CREATE TABLE IF NOT EXISTS ""UserRoles"" (
                ""UserId"" INTEGER NOT NULL,
                ""RoleId"" INTEGER NOT NULL,
                ""AssignedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                PRIMARY KEY (""UserId"", ""RoleId""),
                FOREIGN KEY (""UserId"") REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                FOREIGN KEY (""RoleId"") REFERENCES ""Roles""(""Id"") ON DELETE CASCADE
            );

            -- Índices para mejor performance
            CREATE INDEX IF NOT EXISTS ""IX_UserRoles_UserId"" ON ""UserRoles"" (""UserId"");
            CREATE INDEX IF NOT EXISTS ""IX_UserRoles_RoleId"" ON ""UserRoles"" (""RoleId"");";

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    // Migración 3: Insertar roles por defecto
    private async Task InsertDefaultRolesAsync()
    {
        const string sql = @"
            INSERT INTO ""Roles"" (""Name"", ""Description"") VALUES 
            ('User', 'Regular system user'),
            ('Admin', 'System administrator with full access')
            ON CONFLICT (""Name"") DO NOTHING;";

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    // Método para verificar el estado de la base de datos
    public async Task CheckDatabaseStatusAsync()
    {
        try
        {
            using var conn = _connectionFactory();
            await conn.OpenAsync();
            
            // Verificar si las tablas principales existen
            const string checkSql = @"
                SELECT 
                    EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name = 'Users') as users_table_exists,
                    EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name = '__Migrations') as migrations_table_exists;";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = checkSql;
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var usersExists = reader.GetBoolean(0);
                var migrationsExists = reader.GetBoolean(1);
                
                ConsolePersonalizer.ColorPrint("Database Status:", ConsoleColor.Cyan);
                ConsolePersonalizer.ColorPrint($"  Users table: {(usersExists ? "EXISTS" : "MISSING")}", 
                    usersExists ? ConsoleColor.Green : ConsoleColor.Red);
                ConsolePersonalizer.ColorPrint($"  Migrations table: {(migrationsExists ? "EXISTS" : "MISSING")}", 
                    migrationsExists ? ConsoleColor.Green : ConsoleColor.Red);
            }
        }
        catch (Exception ex)
        {
            ConsolePersonalizer.ColorPrint($"Database connection failed: {ex.Message}", ConsoleColor.Red);
        }
    }
}