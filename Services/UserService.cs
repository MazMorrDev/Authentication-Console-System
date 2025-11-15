namespace AuthenticationConsoleSystem;

using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;

// Servicio para manejar usuarios usando ADO.NET con PostgreSQL
public class UserService(Func<DbConnection> connectionFactory) : IGeneralService<User>
{
    private readonly Func<DbConnection> _connectionFactory = connectionFactory;
    private readonly Logs _logs = new();

    // Obtiene todos los usuarios de la base de datos
    public async Task<List<User>> GetAllAsync()
    {
        const string sql = "SELECT \"Id\", \"UserName\", \"HashPassword\", \"IsLogged\" FROM \"Users\";";
        _logs.LogSql(sql);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var result = new List<User>();
        using var reader = await cmd.ExecuteReaderAsync();

        // Leer cada fila y crear objetos User
        while (await reader.ReadAsync())
        {
            result.Add(new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? null : reader.GetString(reader.GetOrdinal("UserName")),
                HashPassword = reader.IsDBNull(reader.GetOrdinal("HashPassword")) ? null : reader.GetString(reader.GetOrdinal("HashPassword")),
                IsLogged = reader.GetBoolean(reader.GetOrdinal("IsLogged"))
            });
        }
        return result;
    }

    // Busca un usuario por su ID
    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = "SELECT \"Id\", \"UserName\", \"HashPassword\", \"IsLogged\" FROM \"Users\" WHERE \"Id\" = @Id;";
        _logs.LogSql(sql, [("@Id", id)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // Crear parámetro para evitar SQL injection
        var p = cmd.CreateParameter();
        p.ParameterName = "@Id";
        p.Value = id;
        cmd.Parameters.Add(p);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? null : reader.GetString(reader.GetOrdinal("UserName")),
                HashPassword = reader.IsDBNull(reader.GetOrdinal("HashPassword")) ? null : reader.GetString(reader.GetOrdinal("HashPassword")),
                IsLogged = reader.GetBoolean(reader.GetOrdinal("IsLogged"))
            };
        }
        return null; // Usuario no encontrado
    }

    // Crea un nuevo usuario en la base de datos
    public async Task<User> CreateAsync(User user)
    {
        const string sql = @"INSERT INTO ""Users"" (""UserName"", ""HashPassword"", ""IsLogged"") 
                           VALUES (@UserName, @HashPassword, @IsLogged) 
                           RETURNING ""Id"";";
        
        _logs.LogSql(sql, [
            ("@UserName", user.UserName), 
            ("@HashPassword", user.HashPassword),
            ("@IsLogged", user.IsLogged)
        ]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // Crear parámetros con manejo de valores nulos
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@UserName"; p1.Value = (object?)user.UserName ?? DBNull.Value; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@HashPassword"; p2.Value = (object?)user.HashPassword ?? DBNull.Value; cmd.Parameters.Add(p2);
        var p3 = cmd.CreateParameter(); p3.ParameterName = "@IsLogged"; p3.Value = user.IsLogged; cmd.Parameters.Add(p3);

        // Obtener el ID generado automáticamente usando RETURNING (PostgreSQL)
        var newId = await cmd.ExecuteScalarAsync();
        if (newId != null && int.TryParse(newId.ToString(), out var idValue))
        {
            user.Id = idValue;
        }

        return user;
    }

    // Actualiza los datos de un usuario existente
    public async Task<bool> UpdateAsync(User user)
    {
        const string sql = @"UPDATE ""Users"" 
                           SET ""UserName"" = @UserName, ""HashPassword"" = @HashPassword, ""IsLogged"" = @IsLogged 
                           WHERE ""Id"" = @Id;";
        
        _logs.LogSql(sql, [
            ("@Id", user.Id), 
            ("@UserName", user.UserName),
            ("@IsLogged", user.IsLogged)
        ]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // Configurar todos los parámetros
        var pId = cmd.CreateParameter(); pId.ParameterName = "@Id"; pId.Value = user.Id; cmd.Parameters.Add(pId);
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@UserName"; p1.Value = (object?)user.UserName ?? DBNull.Value; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@HashPassword"; p2.Value = (object?)user.HashPassword ?? DBNull.Value; cmd.Parameters.Add(p2);
        var p3 = cmd.CreateParameter(); p3.ParameterName = "@IsLogged"; p3.Value = user.IsLogged; cmd.Parameters.Add(p3);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0; // True si se actualizó al menos un registro
    }

    // Elimina un usuario y sus relaciones de roles
    public async Task<bool> DeleteAsync(int id)
    {
        // Primero eliminar las relaciones en UserRoles, luego el usuario
        const string sqlDelRelations = "DELETE FROM \"UserRoles\" WHERE \"UserId\" = @Id;";
        const string sql = "DELETE FROM \"Users\" WHERE \"Id\" = @Id;";

        _logs.LogSql(sqlDelRelations, [("@Id", id)]);
        _logs.LogSql(sql, [("@Id", id)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();

        // Usar transacción para asegurar que ambas operaciones se completen
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            // Eliminar relaciones de roles primero
            using var cmdRel = conn.CreateCommand();
            cmdRel.Transaction = tx;
            cmdRel.CommandText = sqlDelRelations;
            var pRel = cmdRel.CreateParameter(); pRel.ParameterName = "@Id"; pRel.Value = id; cmdRel.Parameters.Add(pRel);
            await cmdRel.ExecuteNonQueryAsync();

            // Luego eliminar el usuario
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            var p = cmd.CreateParameter(); p.ParameterName = "@Id"; p.Value = id; cmd.Parameters.Add(p);
            var affected = await cmd.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return affected > 0;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // Valida credenciales de usuario (nombre de usuario y contraseña hasheada)
    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        const string sql = @"SELECT ""Id"", ""UserName"", ""HashPassword"", ""IsLogged"" 
                           FROM ""Users"" 
                           WHERE ""UserName"" = @UserName LIMIT 1;";
        
        _logs.LogSql(sql, [("@UserName", username)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var p1 = cmd.CreateParameter(); p1.ParameterName = "@UserName"; p1.Value = username; cmd.Parameters.Add(p1);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var storedHash = reader.IsDBNull(reader.GetOrdinal("HashPassword")) ? null : reader.GetString(reader.GetOrdinal("HashPassword"));
            
            // Verificar la contraseña usando BCrypt
            if (storedHash != null && BCrypt.Net.BCrypt.Verify(password, storedHash))
            {
                return new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? null : reader.GetString(reader.GetOrdinal("UserName")),
                    HashPassword = storedHash,
                    IsLogged = reader.GetBoolean(reader.GetOrdinal("IsLogged"))
                };
            }
        }
        return null; // Credenciales inválidas
    }

    // Registra un nuevo usuario con contraseña hasheada
    public async Task<bool> RegisterUserAsync(string username, string password)
    {
        // Hash de la contraseña
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        // Crear y guardar usuario
        var newUser = new User
        {
            UserName = username,
            HashPassword = hashedPassword,
            IsLogged = false
        };

        try
        {
            await CreateAsync(newUser);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Inicia sesión de usuario
    public async Task<User?> LoginUserAsync(string username, string password)
    {
        var user = await ValidateCredentialsAsync(username, password);
        
        if (user != null)
        {
            // Actualizar estado de login
            user.IsLogged = true;
            await UpdateAsync(user);
            return user;
        }

        return null; // Retorna null si falla la autenticación
    }

    // Cierra sesión de usuario
    public async Task<bool> LogoutUserAsync(int userId)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            user.IsLogged = false;
            return await UpdateAsync(user);
        }
        return false;
    }
}