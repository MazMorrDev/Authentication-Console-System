namespace AuthenticationConsoleSystem;

public interface IGeneralService<T>
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T> CreateAsync(T t);
    Task<bool> UpdateAsync(T t);
    Task<bool> DeleteAsync(int id);
}
