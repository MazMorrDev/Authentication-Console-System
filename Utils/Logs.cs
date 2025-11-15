namespace AuthenticationConsoleSystem;

public class Logs
{
    // Muestra las consultas SQL en consola para debugging
    public void LogSql(string sql, IEnumerable<(string name, object? value)>? parameters = null)
    {
        Console.WriteLine("\n-- SQL ------------------------------------------------");
        Console.WriteLine(sql);

        if (parameters != null)
        {
            foreach (var (name, value) in parameters)
            {
                Console.WriteLine($"-- PARAM: {name} = {value}");
            }
        }
        Console.WriteLine("-- ---------------------------------------------------\n");
    }

}
