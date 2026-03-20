using FluentAssertions;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;

namespace IndustrialDataProcessor.Infrastructure.Tests.Communication.Connection;

/// <summary>
/// ConnectionManager API连接相关测试
/// 测试API连接创建、复用和配置功能
/// </summary>
public class ConnectionManagerApiTests : IAsyncDisposable
{
    private readonly ConnectionManager _connectionManager;

    public ConnectionManagerApiTests()
    {
        _connectionManager = new ConnectionManager();
    }

    public async ValueTask DisposeAsync()
    {
        await _connectionManager.DisposeAsync();
    }

    #region API连接创建测试

    [Fact]
    public async Task GetOrCreateConnectionAsync_WithHttpApiConfig_CreatesHttpApiConnectionHandle()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-001",
            AccessApiString = "http://localhost:5000/api/data",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 5000
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);

        // Assert
        handle.Should().NotBeNull();
        handle.Should().BeOfType<HttpApiConnectionHandle>();
        
        var apiHandle = handle as HttpApiConnectionHandle;
        apiHandle!.AccessApiString.Should().Be("http://localhost:5000/api/data");
        apiHandle.RequestMethod.Should().Be(RequestMethod.Get);
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_WithPostMethod_CreatesCorrectHandle()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-002",
            AccessApiString = "http://localhost:5000/api/submit",
            RequestMethod = RequestMethod.Post,
            ReceiveTimeOut = 10000
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);

        // Assert
        var apiHandle = handle as HttpApiConnectionHandle;
        apiHandle.Should().NotBeNull();
        apiHandle!.RequestMethod.Should().Be(RequestMethod.Post);
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_WithCredentials_CreatesHandleWithAuth()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-003",
            AccessApiString = "http://localhost:5000/api/secure",
            RequestMethod = RequestMethod.Get,
            Account = "admin",
            Password = "password123",
            ReceiveTimeOut = 5000
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);

        // Assert
        var apiHandle = handle as HttpApiConnectionHandle;
        apiHandle.Should().NotBeNull();
        apiHandle!.Account.Should().Be("admin");
        apiHandle.Password.Should().Be("password123");
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_WithGateway_CreatesHandleWithGateway()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-004",
            AccessApiString = "api/data",
            RequestMethod = RequestMethod.Get,
            Gateway = "http://gateway.local:8080",
            ReceiveTimeOut = 5000
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);

        // Assert
        var apiHandle = handle as HttpApiConnectionHandle;
        apiHandle.Should().NotBeNull();
        apiHandle!.Gateway.Should().Be("http://gateway.local:8080");
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_WithNullRequestMethod_DefaultsToGet()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-005",
            AccessApiString = "http://localhost:5000/api/data",
            RequestMethod = null, // 不指定请求方法
            ReceiveTimeOut = 5000
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);

        // Assert
        var apiHandle = handle as HttpApiConnectionHandle;
        apiHandle.Should().NotBeNull();
        apiHandle!.RequestMethod.Should().Be(RequestMethod.Get);
    }

    #endregion

    #region 连接复用测试

    [Fact]
    public async Task GetOrCreateConnectionAsync_SameConfig_ReusesSameConnection()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-REUSE-001",
            AccessApiString = "http://localhost:5000/api/data",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 5000
        };

        // Act
        var handle1 = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);
        var handle2 = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);

        // Assert
        handle1.Should().BeSameAs(handle2);
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_DifferentConfigs_CreatesDifferentConnections()
    {
        // Arrange
        var config1 = new HttpApiInterfaceConfig
        {
            Id = "API-DIFF-001",
            AccessApiString = "http://localhost:5000/api/data1",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 5000
        };

        var config2 = new HttpApiInterfaceConfig
        {
            Id = "API-DIFF-002",
            AccessApiString = "http://localhost:5000/api/data2",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 5000
        };

        // Act
        var handle1 = await _connectionManager.GetOrCreateConnectionAsync(config1, CancellationToken.None);
        var handle2 = await _connectionManager.GetOrCreateConnectionAsync(config2, CancellationToken.None);

        // Assert
        handle1.Should().NotBeSameAs(handle2);
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_ConcurrentRequests_ReturnsSameConnection()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-CONCURRENT-001",
            AccessApiString = "http://localhost:5000/api/data",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 5000
        };

        // Act - 并发创建连接
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None))
            .ToArray();

        var handles = await Task.WhenAll(tasks);

        // Assert - 所有句柄应该是同一个实例
        var firstHandle = handles[0];
        handles.Should().AllSatisfy(h => h.Should().BeSameAs(firstHandle));
    }

    #endregion

    #region HttpClient配置测试

    [Fact]
    public async Task GetOrCreateConnectionAsync_WithTimeout_ConfiguresHttpClientTimeout()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-TIMEOUT-001",
            AccessApiString = "http://localhost:5000/api/data",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 15000 // 15秒超时
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);
        var apiHandle = handle as HttpApiConnectionHandle;
        var httpClient = apiHandle!.GetRawConnection<HttpClient>();

        // Assert
        httpClient.Timeout.Should().Be(TimeSpan.FromMilliseconds(15000));
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_WithZeroTimeout_UsesDefaultTimeout()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-DEFAULT-TIMEOUT-001",
            AccessApiString = "http://localhost:5000/api/data",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 0 // 0表示使用默认值
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);
        var apiHandle = handle as HttpApiConnectionHandle;
        var httpClient = apiHandle!.GetRawConnection<HttpClient>();

        // Assert - 默认超时应该是10秒
        httpClient.Timeout.Should().Be(TimeSpan.FromMilliseconds(10000));
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_ConfiguresJsonAcceptHeader()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-HEADER-001",
            AccessApiString = "http://localhost:5000/api/data",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 5000
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);
        var apiHandle = handle as HttpApiConnectionHandle;
        var httpClient = apiHandle!.GetRawConnection<HttpClient>();

        // Assert
        httpClient.DefaultRequestHeaders.Accept
            .Should().Contain(h => h.MediaType == "application/json");
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_WithCredentials_ConfiguresBasicAuth()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-AUTH-001",
            AccessApiString = "http://localhost:5000/api/secure",
            RequestMethod = RequestMethod.Get,
            Account = "testuser",
            Password = "testpass",
            ReceiveTimeOut = 5000
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);
        var apiHandle = handle as HttpApiConnectionHandle;
        var httpClient = apiHandle!.GetRawConnection<HttpClient>();

        // Assert
        httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Basic");
    }

    [Fact]
    public async Task GetOrCreateConnectionAsync_WithoutCredentials_NoAuthHeader()
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = "API-NO-AUTH-001",
            AccessApiString = "http://localhost:5000/api/public",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 5000
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);
        var apiHandle = handle as HttpApiConnectionHandle;
        var httpClient = apiHandle!.GetRawConnection<HttpClient>();

        // Assert
        httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    #endregion

    #region 清理连接测试

    [Fact]
    public async Task ClearAllConnectionsAsync_DisposesAllConnections()
    {
        // Arrange
        var config1 = new HttpApiInterfaceConfig
        {
            Id = "API-CLEAR-001",
            AccessApiString = "http://localhost:5000/api/data1",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 5000
        };

        var config2 = new HttpApiInterfaceConfig
        {
            Id = "API-CLEAR-002",
            AccessApiString = "http://localhost:5000/api/data2",
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 5000
        };

        var handle1 = await _connectionManager.GetOrCreateConnectionAsync(config1, CancellationToken.None);
        var handle2 = await _connectionManager.GetOrCreateConnectionAsync(config2, CancellationToken.None);

        // Act
        await _connectionManager.ClearAllConnectionsAsync();

        // Assert - 创建新连接应该返回不同的实例
        var newHandle1 = await _connectionManager.GetOrCreateConnectionAsync(config1, CancellationToken.None);
        newHandle1.Should().NotBeSameAs(handle1);
    }

    #endregion

    #region 所有请求方法测试

    [Theory]
    [InlineData(RequestMethod.Get)]
    [InlineData(RequestMethod.Post)]
    [InlineData(RequestMethod.Put)]
    [InlineData(RequestMethod.Delete)]
    public async Task GetOrCreateConnectionAsync_AllRequestMethods_CreatesCorrectHandle(RequestMethod method)
    {
        // Arrange
        var config = new HttpApiInterfaceConfig
        {
            Id = $"API-METHOD-{method}",
            AccessApiString = $"http://localhost:5000/api/{method.ToString().ToLower()}",
            RequestMethod = method,
            ReceiveTimeOut = 5000
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);

        // Assert
        var apiHandle = handle as HttpApiConnectionHandle;
        apiHandle.Should().NotBeNull();
        apiHandle!.RequestMethod.Should().Be(method);
    }

    #endregion
}
