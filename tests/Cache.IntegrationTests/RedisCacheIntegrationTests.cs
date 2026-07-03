using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Cache.IntegrationTests;

// This is the piece the real Jenkinsfile's Docker-in-Docker wiring exists to support - a
// genuine Testcontainers-backed integration test running inside CI, not just unit tests
// mocking the client.
public sealed class RedisCacheIntegrationTests : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7.4-alpine").Build();
    private ConnectionMultiplexer _connection = null!;
    private RedisCache _sut = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
        _sut = new RedisCache(_connection);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Set_Then_Get_Returns_The_Stored_Value()
    {
        await _sut.SetAsync("greeting", "hello-world");

        var result = await _sut.GetAsync("greeting");

        Assert.Equal("hello-world", result);
    }

    [Fact]
    public async Task Get_Returns_Null_For_A_Key_That_Was_Never_Set()
    {
        var result = await _sut.GetAsync("never-set");

        Assert.Null(result);
    }

    [Fact]
    public async Task An_Expired_Key_Is_No_Longer_Retrievable()
    {
        await _sut.SetAsync("short-lived", "value", TimeSpan.FromMilliseconds(200));

        await Task.Delay(TimeSpan.FromMilliseconds(600));

        var result = await _sut.GetAsync("short-lived");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_Removes_The_Key()
    {
        await _sut.SetAsync("to-delete", "value");

        var deleted = await _sut.DeleteAsync("to-delete");
        var afterDelete = await _sut.GetAsync("to-delete");

        Assert.True(deleted);
        Assert.Null(afterDelete);
    }
}
