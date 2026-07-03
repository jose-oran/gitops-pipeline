using NSubstitute;
using StackExchange.Redis;

namespace Cache.UnitTests;

public class RedisCacheTests
{
    private readonly IConnectionMultiplexer _multiplexer = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly RedisCache _sut;

    public RedisCacheTests()
    {
        _multiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_database);
        _sut = new RedisCache(_multiplexer);
    }

    [Fact]
    public async Task SetAsync_Delegates_To_StringSetAsync_With_The_Given_Expiry()
    {
        var expiry = TimeSpan.FromMinutes(5);

        await _sut.SetAsync("key", "value", expiry);

        await _database.Received(1).StringSetAsync((RedisKey)"key", (RedisValue)"value", expiry, When.Always);
    }

    [Fact]
    public async Task GetAsync_Returns_Null_When_The_Key_Is_Missing()
    {
        _database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);

        var result = await _sut.GetAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_Returns_The_Stored_Value()
    {
        _database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns((RedisValue)"hello");

        var result = await _sut.GetAsync("key");

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task DeleteAsync_Delegates_To_KeyDeleteAsync()
    {
        _database.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);

        var result = await _sut.DeleteAsync("key");

        Assert.True(result);
    }
}
