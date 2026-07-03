using StackExchange.Redis;

namespace Cache;

public sealed class RedisCache(IConnectionMultiplexer connectionMultiplexer) : ICache
{
    private IDatabase Database => connectionMultiplexer.GetDatabase();

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null) =>
        await Database.StringSetAsync(key, value, expiry, When.Always);

    public async Task<string?> GetAsync(string key)
    {
        var value = await Database.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task<bool> DeleteAsync(string key) => await Database.KeyDeleteAsync(key);
}
