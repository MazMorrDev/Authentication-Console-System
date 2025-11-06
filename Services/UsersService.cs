namespace AuthenticationConsoleSystem;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

/// <summary>
/// UsersService using raw SQL (ADO.NET) instead of Entity Framework.
/// The service accepts a factory that returns an openable <see cref="DbConnection"/>.
/// It prints the SQL statements to the console before executing them so you can see the queries.
///
/// Example wiring (SQLite):
///  - dotnet add package Microsoft.Data.Sqlite
///  - var factory = new Func<DbConnection>(() => new SqliteConnection("Data Source=app.db"));
///  - var usersService = new UsersService(factory);
///
/// This keeps the implementation provider-agnostic (uses DbConnection), but the examples assume
/// typical SQL syntax. If you target a different provider you may need to adjust small SQL bits.
/// </summary>
public class UsersService
{
	private readonly Func<DbConnection> _connectionFactory;

	public UsersService(Func<DbConnection> connectionFactory)
	{
		_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
	}

	private void LogSql(string sql, IEnumerable<(string name, object? value)>? parameters = null)
	{
		Console.WriteLine("-- SQL ------------------------------------------------");
		Console.WriteLine(sql);
		if (parameters != null)
		{
			foreach (var p in parameters)
			{
				Console.WriteLine($"-- PARAM: {p.name} = {p.value}");
			}
		}
		Console.WriteLine("-- ---------------------------------------------------");
	}

	public async Task<List<Users>> GetAllAsync()
	{
		var sql = "SELECT Id, UserName, Email, Password FROM Users;";
		LogSql(sql);

		using var conn = _connectionFactory();
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;

		var result = new List<Users>();
		using var reader = await cmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			result.Add(new Users
			{
				Id = reader.GetInt32(0),
				UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
				Email = reader.IsDBNull(2) ? null : reader.GetString(2),
				Password = reader.IsDBNull(3) ? null : reader.GetString(3),
			});
		}
		return result;
	}

	public async Task<Users?> GetByIdAsync(int id)
	{
		var sql = "SELECT Id, UserName, Email, Password FROM Users WHERE Id = @Id;";
		LogSql(sql, new[] { ("@Id", (object?)id) });

		using var conn = _connectionFactory();
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;
		var p = cmd.CreateParameter(); p.ParameterName = "@Id"; p.Value = id; cmd.Parameters.Add(p);

		using var reader = await cmd.ExecuteReaderAsync();
		if (await reader.ReadAsync())
		{
			return new Users
			{
				Id = reader.GetInt32(0),
				UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
				Email = reader.IsDBNull(2) ? null : reader.GetString(2),
				Password = reader.IsDBNull(3) ? null : reader.GetString(3),
			};
		}
		return null;
	}

	public async Task<Users> CreateAsync(Users user)
	{
		var sql = "INSERT INTO Users (UserName, Email, Password) VALUES (@UserName, @Email, @Password);";
		LogSql(sql, new[] { ("@UserName", (object?)user.UserName), ("@Email", (object?)user.Email), ("@Password", (object?)user.Password) });

		using var conn = _connectionFactory();
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;

		var p1 = cmd.CreateParameter(); p1.ParameterName = "@UserName"; p1.Value = (object?)user.UserName ?? DBNull.Value; cmd.Parameters.Add(p1);
		var p2 = cmd.CreateParameter(); p2.ParameterName = "@Email"; p2.Value = (object?)user.Email ?? DBNull.Value; cmd.Parameters.Add(p2);
		var p3 = cmd.CreateParameter(); p3.ParameterName = "@Password"; p3.Value = (object?)user.Password ?? DBNull.Value; cmd.Parameters.Add(p3);

		await cmd.ExecuteNonQueryAsync();

		// Try to obtain last insert id for SQLite (common in local CLI apps). If provider doesn't support it,
		// callers can ignore Id or you can adapt this to your RDBMS.
		try
		{
			using var cmdId = conn.CreateCommand();
			cmdId.CommandText = "SELECT last_insert_rowid();"; // SQLite specific
			LogSql(cmdId.CommandText);
			var idObj = await cmdId.ExecuteScalarAsync();
			if (idObj != null && int.TryParse(idObj.ToString(), out var newId))
			{
				user.Id = newId;
			}
		}
		catch
		{
			// ignore: provider may not support last_insert_rowid(); caller can fetch by other unique fields
		}

		return user;
	}

	public async Task<bool> UpdateAsync(Users user)
	{
		var sql = "UPDATE Users SET UserName = @UserName, Email = @Email, Password = @Password WHERE Id = @Id;";
		LogSql(sql, new[] { ("@Id", (object?)user.Id), ("@UserName", (object?)user.UserName), ("@Email", (object?)user.Email) });

		using var conn = _connectionFactory();
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;

		var pId = cmd.CreateParameter(); pId.ParameterName = "@Id"; pId.Value = user.Id; cmd.Parameters.Add(pId);
		var p1 = cmd.CreateParameter(); p1.ParameterName = "@UserName"; p1.Value = (object?)user.UserName ?? DBNull.Value; cmd.Parameters.Add(p1);
		var p2 = cmd.CreateParameter(); p2.ParameterName = "@Email"; p2.Value = (object?)user.Email ?? DBNull.Value; cmd.Parameters.Add(p2);
		var p3 = cmd.CreateParameter(); p3.ParameterName = "@Password"; p3.Value = (object?)user.Password ?? DBNull.Value; cmd.Parameters.Add(p3);

		var affected = await cmd.ExecuteNonQueryAsync();
		return affected > 0;
	}

	public async Task<bool> DeleteAsync(int id)
	{
		var sqlDelRelations = "DELETE FROM UserRoles WHERE UserId = @Id;";
		var sql = "DELETE FROM Users WHERE Id = @Id;";

		LogSql(sqlDelRelations, new[] { ("@Id", (object?)id) });
		LogSql(sql, new[] { ("@Id", (object?)id) });

		using var conn = _connectionFactory();
		await conn.OpenAsync();

		using (var tx = conn.BeginTransaction())
		{
			using var cmdRel = conn.CreateCommand();
			cmdRel.Transaction = tx;
			cmdRel.CommandText = sqlDelRelations;
			var pRel = cmdRel.CreateParameter(); pRel.ParameterName = "@Id"; pRel.Value = id; cmdRel.Parameters.Add(pRel);
			await cmdRel.ExecuteNonQueryAsync();

			using var cmd = conn.CreateCommand();
			cmd.Transaction = tx;
			cmd.CommandText = sql;
			var p = cmd.CreateParameter(); p.ParameterName = "@Id"; p.Value = id; cmd.Parameters.Add(p);
			var affected = await cmd.ExecuteNonQueryAsync();
			tx.Commit();
			return affected > 0;
		}
	}

	public async Task<List<Roles>> GetRolesForUserAsync(int userId)
	{
		var sql = "SELECT r.Id, r.Name FROM Roles r INNER JOIN UserRoles ur ON r.Id = ur.RoleId WHERE ur.UserId = @UserId;";
		LogSql(sql, new[] { ("@UserId", (object?)userId) });

		using var conn = _connectionFactory();
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;
		var p = cmd.CreateParameter(); p.ParameterName = "@UserId"; p.Value = userId; cmd.Parameters.Add(p);

		var result = new List<Roles>();
		using var reader = await cmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			result.Add(new Roles { Id = reader.GetInt32(0), Name = reader.IsDBNull(1) ? null : reader.GetString(1) });
		}
		return result;
	}

	public async Task<bool> AssignRoleAsync(int userId, int roleId)
	{
		var sqlCheck = "SELECT COUNT(1) FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId;";
		var sqlInsert = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);";
		LogSql(sqlCheck, new[] { ("@UserId", (object?)userId), ("@RoleId", (object?)roleId) });

		using var conn = _connectionFactory();
		await conn.OpenAsync();
		using var cmdCheck = conn.CreateCommand();
		cmdCheck.CommandText = sqlCheck;
		var p1 = cmdCheck.CreateParameter(); p1.ParameterName = "@UserId"; p1.Value = userId; cmdCheck.Parameters.Add(p1);
		var p2 = cmdCheck.CreateParameter(); p2.ParameterName = "@RoleId"; p2.Value = roleId; cmdCheck.Parameters.Add(p2);
		var exists = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync()) > 0;
		if (exists) return false;

		using var cmd = conn.CreateCommand();
		cmd.CommandText = sqlInsert;
		var q1 = cmd.CreateParameter(); q1.ParameterName = "@UserId"; q1.Value = userId; cmd.Parameters.Add(q1);
		var q2 = cmd.CreateParameter(); q2.ParameterName = "@RoleId"; q2.Value = roleId; cmd.Parameters.Add(q2);
		var affected = await cmd.ExecuteNonQueryAsync();
		return affected > 0;
	}

	public async Task<bool> RemoveRoleAsync(int userId, int roleId)
	{
		var sql = "DELETE FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId;";
		LogSql(sql, new[] { ("@UserId", (object?)userId), ("@RoleId", (object?)roleId) });

		using var conn = _connectionFactory();
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;
		var p1 = cmd.CreateParameter(); p1.ParameterName = "@UserId"; p1.Value = userId; cmd.Parameters.Add(p1);
		var p2 = cmd.CreateParameter(); p2.ParameterName = "@RoleId"; p2.Value = roleId; cmd.Parameters.Add(p2);
		var affected = await cmd.ExecuteNonQueryAsync();
		return affected > 0;
	}

	public async Task<Users?> ValidateCredentialsAsync(string usernameOrEmail, string password)
	{
		var sql = "SELECT Id, UserName, Email, Password FROM Users WHERE (UserName = @u OR Email = @u) AND Password = @p LIMIT 1;";
		// LIMIT is SQLite/MySQL; other DBs may need different syntax (TOP 1 or FETCH FIRST 1 ROWS ONLY).
		LogSql(sql, new[] { ("@u", (object?)usernameOrEmail), ("@p", (object?)password) });

		using var conn = _connectionFactory();
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;
		var p1 = cmd.CreateParameter(); p1.ParameterName = "@u"; p1.Value = usernameOrEmail; cmd.Parameters.Add(p1);
		var p2 = cmd.CreateParameter(); p2.ParameterName = "@p"; p2.Value = password; cmd.Parameters.Add(p2);

		using var reader = await cmd.ExecuteReaderAsync();
		if (await reader.ReadAsync())
		{
			return new Users
			{
				Id = reader.GetInt32(0),
				UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
				Email = reader.IsDBNull(2) ? null : reader.GetString(2),
				Password = reader.IsDBNull(3) ? null : reader.GetString(3),
			};
		}
		return null;
	}
}