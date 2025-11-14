namespace AuthenticationConsoleSystem;

using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;


// Servicio para manejar usuarios usando ADO.NET (sin Entity Framework)
// Acepta una fábrica de conexiones a base de datos
public class UserService(Func<DbConnection> connectionFactory) : IGeneralService<User>
{
    private readonly Func<DbConnection> _connectionFactory = connectionFactory;
    private readonly Logs _logs = new();


    // Obtiene todos los usuarios de la base de datos
    public async Task<List<User>> GetAllAsync()
    {
        string sql = "SELECT * FROM Users;";
        _logs.LogSql(sql);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var result = new List<User>();
        using var reader = await cmd.ExecuteReaderAsync();
        
        // Leer cada fila y crear objetos Users
        while (await reader.ReadAsync())
        {
            result.Add(new User
            {
                Id = reader.GetInt32(0),
                UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
                Email = reader.IsDBNull(2) ? null : reader.GetString(2),
                HashPassword = reader.IsDBNull(3) ? null : reader.GetString(3),
            });
        }
        return result;
    }

    // Busca un usuario por su ID
    public async Task<User?> GetByIdAsync(int id)
    {
        var sql = $"SELECT * FROM Users WHERE Id = @Id;";
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
                Id = reader.GetInt32(0),
                UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
                Email = reader.IsDBNull(2) ? null : reader.GetString(2),
                HashPassword = reader.IsDBNull(3) ? null : reader.GetString(3),
            };
        }
        return null; // Usuario no encontrado
    }


    // Crea un nuevo usuario en la base de datos
    public async Task<User> CreateAsync(User user)
    {
        var sql = $"INSERT INTO Users (UserName, Email, Password) VALUES (@UserName, @Email, @Password);";
        _logs.LogSql(sql, [("@UserName", user.UserName), ("@Email", user.Email), ("@Password", user.HashPassword)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // Crear parámetros con manejo de valores nulos
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@UserName"; p1.Value = (object?)user.UserName ?? DBNull.Value; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@Email"; p2.Value = (object?)user.Email ?? DBNull.Value; cmd.Parameters.Add(p2);
        var p3 = cmd.CreateParameter(); p3.ParameterName = "@Password"; p3.Value = (object?)user.HashPassword ?? DBNull.Value; cmd.Parameters.Add(p3);

        await cmd.ExecuteNonQueryAsync();

        // Obtener el ID generado automáticamente (funciona en SQLite)
        try
        {
            using var cmdId = conn.CreateCommand();
            cmdId.CommandText = "SELECT last_insert_rowid();";
            _logs.LogSql(cmdId.CommandText);
            var idObj = await cmdId.ExecuteScalarAsync();
            if (idObj != null && int.TryParse(idObj.ToString(), out var newId))
            {
                user.Id = newId;
            }
        }
        catch
        {
            // Si falla, el ID queda en 0
        }

        return user;
    }


    // Actualiza los datos de un usuario existente
    public async Task<bool> UpdateAsync(User user)
    {
        var sql = "UPDATE Users SET UserName = @UserName, Email = @Email, Password = @Password WHERE Id = @Id;";
        _logs.LogSql(sql, [("@Id", user.Id), ("@UserName", user.UserName), ("@Email", user.Email)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // Configurar todos los parámetros
        var pId = cmd.CreateParameter(); pId.ParameterName = "@Id"; pId.Value = user.Id; cmd.Parameters.Add(pId);
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@UserName"; p1.Value = (object?)user.UserName ?? DBNull.Value; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@Email"; p2.Value = (object?)user.Email ?? DBNull.Value; cmd.Parameters.Add(p2);
        var p3 = cmd.CreateParameter(); p3.ParameterName = "@Password"; p3.Value = (object?)user.HashPassword ?? DBNull.Value; cmd.Parameters.Add(p3);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0; // True si se actualizó al menos un registro
    }


    // Elimina un usuario y sus relaciones de roles
    public async Task<bool> DeleteAsync(int id)
    {
        // Primero eliminar las relaciones en UserRoles, luego el usuario
        var sqlDelRelations = "DELETE FROM UserRoles WHERE UserId = @Id;";
        var sql = "DELETE FROM Users WHERE Id = @Id;";

        _logs.LogSql(sqlDelRelations, [("@Id", id)]);
        _logs.LogSql(sql, [("@Id", id)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();

        // Usar transacción para asegurar que ambas operaciones se completen
        using var tx = conn.BeginTransaction();
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

        tx.Commit();
        return affected > 0;
    }


    // Obtiene los roles asignados a un usuario
    public async Task<List<Role>> GetRolesForUserAsync(int userId)
    {
        var sql = @"SELECT r.Id, r.Name 
                    FROM Roles r 
                    INNER JOIN UserRoles ur ON r.Id = ur.RoleId 
                    WHERE ur.UserId = @UserId;";
        _logs.LogSql(sql, [("@UserId", userId)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var p = cmd.CreateParameter(); p.ParameterName = "@UserId"; p.Value = userId; cmd.Parameters.Add(p);

        var result = new List<Role>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Role { 
                Id = reader.GetInt32(0), 
                Name = reader.IsDBNull(1) ? null : reader.GetString(1) 
            });
        }
        return result;
    }


    // Asigna un rol a un usuario (si no lo tiene ya)
    public async Task<bool> AssignRoleAsync(int userId, int roleId)
    {
        // Primero verificar si ya tiene el rol
        var sqlCheck = "SELECT COUNT(1) FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId;";
        var sqlInsert = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);";
        _logs.LogSql(sqlCheck, [("@UserId", userId), ("@RoleId", roleId)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        
        // Verificar si ya existe la relación
        using var cmdCheck = conn.CreateCommand();
        cmdCheck.CommandText = sqlCheck;
        var p1 = cmdCheck.CreateParameter(); p1.ParameterName = "@UserId"; p1.Value = userId; cmdCheck.Parameters.Add(p1);
        var p2 = cmdCheck.CreateParameter(); p2.ParameterName = "@RoleId"; p2.Value = roleId; cmdCheck.Parameters.Add(p2);
        var exists = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync()) > 0;
        
        if (exists) return false; // Ya tiene el rol

        // Insertar nueva relación
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sqlInsert;
        var q1 = cmd.CreateParameter(); q1.ParameterName = "@UserId"; q1.Value = userId; cmd.Parameters.Add(q1);
        var q2 = cmd.CreateParameter(); q2.ParameterName = "@RoleId"; q2.Value = roleId; cmd.Parameters.Add(q2);
        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }


    // Remueve un rol de un usuario
    public async Task<bool> RemoveRoleAsync(int userId, int roleId)
    {
        var sql = "DELETE FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId;";
        _logs.LogSql(sql, [("@UserId", userId), ("@RoleId", roleId)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@UserId"; p1.Value = userId; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@RoleId"; p2.Value = roleId; cmd.Parameters.Add(p2);
        
        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }


    // Valida credenciales de usuario (nombre de usuario/email y contraseña)
    public async Task<User?> ValidateCredentialsAsync(string usernameOrEmail, string password)
    {
        var sql = "SELECT Id, UserName, Email, Password FROM Users WHERE (UserName = @u OR Email = @u) AND Password = @p LIMIT 1;";
        _logs.LogSql(sql, [("@u", usernameOrEmail), ("@p", password)]);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@u"; p1.Value = usernameOrEmail; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@p"; p2.Value = password; cmd.Parameters.Add(p2);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
                Email = reader.IsDBNull(2) ? null : reader.GetString(2),
                HashPassword = reader.IsDBNull(3) ? null : reader.GetString(3),
            };
        }
        return null; // Credenciales inválidas
    }
}