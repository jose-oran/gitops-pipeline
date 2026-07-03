namespace Cache;

public interface ICache
{
    Task SetAsync(string key, string value, TimeSpan? expiry = null);

    Task<string?> GetAsync(string key);

    Task<bool> DeleteAsync(string key);
}
