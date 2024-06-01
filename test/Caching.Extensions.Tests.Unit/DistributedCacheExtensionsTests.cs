using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using System.Text;
using System.Text.Json;

namespace Caching.Extensions.Tests.Unit;

public class DistributedCacheExtensionsTests
{
    private readonly Faker _faker;
    private readonly Faker<TestObject> _testObjects;
    private readonly IDistributedCache _distributedCache;

    public DistributedCacheExtensionsTests()
    {
        _faker = new();
        _testObjects = new Faker<TestObject>()
            .RuleFor(o => o.Id, f => f.Random.Int())
            .RuleFor(o => o.Name, f => f.Random.Word());

        _distributedCache = Substitute.For<IDistributedCache>();
    }

    [Fact]
    public void Get_WhenKeyExistsInCache_ShouldReturnObject()
    {
        // Arrange
        var key = _faker.Random.Word();
        var expected = _testObjects.Generate();
        var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expected));

        _distributedCache.Get(key)
            .Returns(cachedData);

        // Act
        var result = _distributedCache.Get<TestObject>(key);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Get_WhenKeyDoesNotExistInCache_ShouldReturnDefault()
    {
        // Arrange
        var key = _faker.Random.Word();

        _distributedCache.Get(key)
            .ReturnsNull();

        // Act
        var result = _distributedCache.Get<TestObject>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenKeyExistsInCache_ShouldReturnObject()
    {
        // Arrange
        var key = _faker.Random.Word();
        var expected = _testObjects.Generate();
        var cancellationToken = CancellationToken.None;
        var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expected));

        _distributedCache.GetAsync(key, cancellationToken)
            .Returns(cachedData);

        // Act
        var result = await _distributedCache.GetAsync<TestObject>(key, cancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExistInCache_ShouldReturnDefault()
    {
        // Arrange
        var key = _faker.Random.Word();
        var cancellationToken = CancellationToken.None;

        _distributedCache.GetAsync(key, cancellationToken)
            .ReturnsNull();

        // Act
        var result = await _distributedCache.GetAsync<TestObject>(key, cancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenKeyExists_ShouldReturnCachedItem()
    {
        // Arrange
        var key = _faker.Random.Word();
        var expected = _testObjects.Generate();
        var cancellationToken = CancellationToken.None;
        var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expected));

        _distributedCache.GetAsync(key, cancellationToken)
            .Returns(cachedData);

        // Act
        var result = await _distributedCache.GetOrCreateAsync(
            key,
            options => Task.FromResult(_testObjects.Generate()),
            cancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenKeyDoesNotExist_ShouldCreateAndCacheItem()
    {
        // Arrange
        var key = _faker.Random.Word();
        var expected = _testObjects.Generate();
        var cancellationToken = CancellationToken.None;

        _distributedCache.GetAsync(key, cancellationToken)
            .ReturnsNull();

        // Act
        var result = await _distributedCache.GetOrCreateAsync(
            key,
            options => Task.FromResult(expected),
            cancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
        await _distributedCache.Received(1).SetAsync(
            key,
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            cancellationToken);
    }
    
    [Fact]
    public void Set_WhenAbsoluteExpirationRelativeToNowIsInformed_ShouldSetAbsoluteExpirationRelativeToNow()
    {
        // Arrange
        var key = _faker.Random.Word();
        TestObject? value = null;
        var options = new DistributedCacheEntryOptions();
        var absoluteExpirationRelativeToNow = TimeSpan.Zero;
        var serializedValue = JsonSerializer.Serialize(value);

        // Act
        Action act = () => _distributedCache.Set(key, value, absoluteExpirationRelativeToNow);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Where(x => x.ParamName == "value");

        _distributedCache.Received(0).Set(
            key,
            Arg.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == serializedValue),
            Arg.Is<DistributedCacheEntryOptions>(x => x.AbsoluteExpirationRelativeToNow == absoluteExpirationRelativeToNow));
    }
    
    [Fact]
    public void Set_WhenAbsoluteExpirationIsInformed_ShouldSetAbsoluteExpiration()
    {
        // Arrange
        var key = _faker.Random.Word();
        TestObject? value = null;
        var options = new DistributedCacheEntryOptions();
        var absoluteExpiration = DateTimeOffset.MinValue;
        var serializedValue = JsonSerializer.Serialize(value);

        // Act
        Action act = () => _distributedCache.Set(key, value, absoluteExpiration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Where(x => x.ParamName == "value");

        _distributedCache.Received(0).Set(
            key,
            Arg.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == serializedValue),
            Arg.Is<DistributedCacheEntryOptions>(x => x.AbsoluteExpiration == absoluteExpiration));
    }

    [Fact]
    public void Set_WhenValueIsNotNull_ShouldCacheItem()
    {
        // Arrange
        var key = _faker.Random.Word();
        var value = _testObjects.Generate();
        var options = new DistributedCacheEntryOptions();
        var serializedValue = JsonSerializer.Serialize(value);

        // Act
        _distributedCache.Set(key, value, options);

        // Assert
        _distributedCache.Received(1).Set(
            key,
            Arg.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == serializedValue),
            options);
    }

    [Fact]
    public void Set_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = _faker.Random.Word();
        TestObject? value = null;
        var options = new DistributedCacheEntryOptions();
        var serializedValue = JsonSerializer.Serialize(value);

        // Act
        Action act = () => _distributedCache.Set(key, value, options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Where(x => x.ParamName == "value");

        _distributedCache.Received(0).Set(
            key,
            Arg.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == serializedValue),
            options);
    }

    [Fact]
    public async Task SetAsync_WhenAbsoluteExpirationRelativeToNowIsInformed_ShouldSetAbsoluteExpirationRelativeToNow()
    {
        // Arrange
        var key = _faker.Random.Word();
        TestObject? value = null;
        var absoluteExpirationRelativeToNow = TimeSpan.Zero;
        var cancellationToken = CancellationToken.None;
        var serializedValue = JsonSerializer.Serialize(value);

        // Act
        Func<Task> act = () => _distributedCache.SetAsync(key, value, absoluteExpirationRelativeToNow, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(x => x.ParamName == "value");

        await _distributedCache.Received(0).SetAsync(
            key,
            Arg.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == serializedValue),
            Arg.Is<DistributedCacheEntryOptions>(x => x.AbsoluteExpirationRelativeToNow == absoluteExpirationRelativeToNow),
            cancellationToken);
    }
    
    [Fact]
    public async Task SetAsync_WhenAbsoluteExpirationIsInformed_ShouldSetAbsoluteExpiration()
    {
        // Arrange
        var key = _faker.Random.Word();
        TestObject? value = null;
        var absoluteExpiration = DateTimeOffset.MinValue;
        var cancellationToken = CancellationToken.None;
        var serializedValue = JsonSerializer.Serialize(value);

        // Act
        Func<Task> act = () => _distributedCache.SetAsync(key, value, absoluteExpiration, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(x => x.ParamName == "value");

        await _distributedCache.Received(0).SetAsync(
            key,
            Arg.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == serializedValue),
            Arg.Is<DistributedCacheEntryOptions>(x => x.AbsoluteExpiration == absoluteExpiration),
            cancellationToken);
    }

    [Fact]
    public async Task SetAsync_WhenValueIsNotNull_ShouldCacheItem()
    {
        // Arrange
        var key = _faker.Random.Word();
        var value = _testObjects.Generate();
        var options = new DistributedCacheEntryOptions();
        var cancellationToken = CancellationToken.None;
        var serializedValue = JsonSerializer.Serialize(value);

        // Act
        await _distributedCache.SetAsync(key, value, options, cancellationToken);

        // Assert
        await _distributedCache.Received(1).SetAsync(
            key,
            Arg.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == serializedValue),
            options,
            cancellationToken);
    }

    [Fact]
    public async Task SetAsync_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = _faker.Random.Word();
        TestObject? value = null;
        var options = new DistributedCacheEntryOptions();
        var cancellationToken = CancellationToken.None;
        var serializedValue = JsonSerializer.Serialize(value);

        // Act
        Func<Task> act = () => _distributedCache.SetAsync(key, value, options, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(x => x.ParamName == "value");

        await _distributedCache.Received(0).SetAsync(
            key,
            Arg.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes) == serializedValue),
            options,
            cancellationToken);
    }
}

internal class TestObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}