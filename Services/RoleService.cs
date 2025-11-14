using System.Data.Common;

namespace AuthenticationConsoleSystem;


// Servicio para manejar roles usando ADO.NET (sin Entity Framework)
// ACepta una fábrica de conexiones a base de datos
public class RoleService(Func<DbConnection> connectionFactory) : IGeneralService<Role>
{
    private readonly Func<DbConnection> _connectionFactory = connectionFactory;
    private readonly Logs _logs = new();

    // Obtiene todos los roles de la base de datos
    public async Task<List<Role>> GetAllAsync()
    {
        string sql = "SELECT * FROM Roles;";
        _logs.LogSql(sql);

        using DbConnection connection = _connectionFactory();
        await connection.OpenAsync();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<Role>();
        using DbDataReader reader = await command.ExecuteReaderAsync();

        // Leer cada fila y crear objetos Users
        while (await reader.ReadAsync())
        {
            result.Add(new Role(reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1))
            );
        }
        return result;
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        string sql = "SELECT * FROM Roles WHERE Id = @Id;";
        _logs.LogSql(sql);

        using DbConnection connection = _connectionFactory();
        await connection.OpenAsync();

        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;

        //  Usar parámetros para evitar SQL injection
        DbParameter idParam = command.CreateParameter();
        idParam.ParameterName = "@Id";
        idParam.Value = id;
        command.Parameters.Add(idParam);

        using DbDataReader reader = await command.ExecuteReaderAsync();

        //  Verificar si hay resultados
        if (!await reader.ReadAsync())
        {
            return null;
        }

        //  Usar nombres de columnas en lugar de índices fijos
        return new Role(
            reader.GetInt32(reader.GetOrdinal("Id")),
            reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name"))
        );
    }

    public Task<bool> UpdateAsync(Role t)
    {
        throw new NotImplementedException();
    }

    public Task<Role> CreateAsync(Role t)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }
}

