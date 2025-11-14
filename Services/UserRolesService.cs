namespace AuthenticationConsoleSystem;

using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

public class UserRoleService(Func<DbConnection> connectionFactory) : IGeneralService<UserRole>
{
    private readonly Func<DbConnection> _connectionFactory = connectionFactory;
    private readonly Logs _logs = new();

    public async Task<List<UserRole>> GetAllAsync()
    {
        const string sql = @"SELECT ur.Id, ur.UserId, ur.RoleId, u.UserName, r.Name as RoleName
                            FROM UserRoles ur
                            LEFT JOIN Users u ON ur.UserId = u.Id
                            LEFT JOIN Roles r ON ur.RoleId = r.Id;";
        
        _logs.LogSql(sql);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var result = new List<UserRole>();
        using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            result.Add(new UserRole
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId"))
            });
        }
        return result;
    }

    public async Task<UserRole?> GetByIdAsync(int id)
    {
        const string sql = @"SELECT Id, UserId, RoleId 
                            FROM UserRoles 
                            WHERE Id = @Id;";
        
        _logs.LogSql(sql);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        
        var param = cmd.CreateParameter();
        param.ParameterName = "@Id";
        param.Value = id;
        cmd.Parameters.Add(param);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new UserRole
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId"))
            };
        }
        return null;
    }

    public async Task<UserRole> CreateAsync(UserRole userRole)
    {
        const string sql = @"INSERT INTO UserRoles (UserId, RoleId) 
                            VALUES (@UserId, @RoleId);
                            SELECT last_insert_rowid();";
        
        _logs.LogSql(sql);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // Parámetros
        var userIdParam = cmd.CreateParameter();
        userIdParam.ParameterName = "@UserId";
        userIdParam.Value = userRole.UserId;
        cmd.Parameters.Add(userIdParam);

        var roleIdParam = cmd.CreateParameter();
        roleIdParam.ParameterName = "@RoleId";
        roleIdParam.Value = userRole.RoleId;
        cmd.Parameters.Add(roleIdParam);

        // Ejecutar y obtener ID
        var newId = await cmd.ExecuteScalarAsync();
        if (newId != null && int.TryParse(newId.ToString(), out int id))
        {
            userRole.Id = id;
        }

        return userRole;
    }

    public async Task<bool> UpdateAsync(UserRole userRole)
    {
        const string sql = @"UPDATE UserRoles 
                            SET UserId = @UserId, RoleId = @RoleId 
                            WHERE Id = @Id;";
        
        _logs.LogSql(sql);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // Parámetros
        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "@Id";
        idParam.Value = userRole.Id;
        cmd.Parameters.Add(idParam);

        var userIdParam = cmd.CreateParameter();
        userIdParam.ParameterName = "@UserId";
        userIdParam.Value = userRole.UserId;
        cmd.Parameters.Add(userIdParam);

        var roleIdParam = cmd.CreateParameter();
        roleIdParam.ParameterName = "@RoleId";
        roleIdParam.Value = userRole.RoleId;
        cmd.Parameters.Add(roleIdParam);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM UserRoles WHERE Id = @Id;";
        _logs.LogSql(sql);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        
        var param = cmd.CreateParameter();
        param.ParameterName = "@Id";
        param.Value = id;
        cmd.Parameters.Add(param);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    // Métodos específicos para UserRoles
    public async Task<List<UserRole>> GetByUserIdAsync(int userId)
    {
        const string sql = @"SELECT Id, UserId, RoleId 
                            FROM UserRoles 
                            WHERE UserId = @UserId;";
        
        _logs.LogSql(sql);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        
        var param = cmd.CreateParameter();
        param.ParameterName = "@UserId";
        param.Value = userId;
        cmd.Parameters.Add(param);

        var result = new List<UserRole>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new UserRole
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId"))
            });
        }
        return result;
    }

    public async Task<List<UserRole>> GetByRoleIdAsync(int roleId)
    {
        const string sql = @"SELECT Id, UserId, RoleId 
                            FROM UserRoles 
                            WHERE RoleId = @RoleId;";
        
        _logs.LogSql(sql);

        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        
        var param = cmd.CreateParameter();
        param.ParameterName = "@RoleId";
        param.Value = roleId;
        cmd.Parameters.Add(param);

        var result = new List<UserRole>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new UserRole
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId"))
            });
        }
        return result;
    }
}