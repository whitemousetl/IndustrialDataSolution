using FluentAssertions;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;

namespace IndustrialDataProcessor.Infrastructure.Tests.Communication.Connection;

/// <summary>
/// HttpApiConnectionHandle 单元测试
/// 测试HTTP API连接句柄的功能
/// </summary>
public class HttpApiConnectionHandleTests
{
    #region 创建和初始化测试

    [Fact]
    public void Constructor_WithValidParameters_CreatesHandleSuccessfully()
    {
        // Arrange
        var httpClient = new HttpClient();
        var accessApiString = "http://localhost:5000/api/data";
        var requestMethod = RequestMethod.Get;

        // Act
        var handle = new HttpApiConnectionHandle(httpClient, accessApiString, requestMethod);

        // Assert
        handle.Should().NotBeNull();
        handle.AccessApiString.Should().Be(accessApiString);
        handle.RequestMethod.Should().Be(requestMethod);
        handle.Account.Should().BeNull();
        handle.Password.Should().BeNull();
        handle.Gateway.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_CreatesHandleWithAllProperties()
    {
        // Arrange
        var httpClient = new HttpClient();
        var accessApiString = "http://localhost:5000/api/data";
        var requestMethod = RequestMethod.Post;
        var account = "admin";
        var password = "password123";
        var gateway = "http://gateway.local";

        // Act
        var handle = new HttpApiConnectionHandle(
            httpClient, accessApiString, requestMethod, 
            account, password, gateway);

        // Assert
        handle.AccessApiString.Should().Be(accessApiString);
        handle.RequestMethod.Should().Be(requestMethod);
        handle.Account.Should().Be(account);
        handle.Password.Should().Be(password);
        handle.Gateway.Should().Be(gateway);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new HttpApiConnectionHandle(null!, "http://localhost", RequestMethod.Get);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullAccessApiString_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var act = () => new HttpApiConnectionHandle(httpClient, null!, RequestMethod.Get);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("accessApiString");
    }

    [Theory]
    [InlineData(RequestMethod.Get)]
    [InlineData(RequestMethod.Post)]
    [InlineData(RequestMethod.Put)]
    [InlineData(RequestMethod.Delete)]
    public void Constructor_WithDifferentRequestMethods_StoresCorrectMethod(RequestMethod method)
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var handle = new HttpApiConnectionHandle(httpClient, "http://localhost", method);

        // Assert
        handle.RequestMethod.Should().Be(method);
    }

    #endregion

    #region GetRawConnection 测试

    [Fact]
    public void GetRawConnection_WithCorrectType_ReturnsHttpClient()
    {
        // Arrange
        var httpClient = new HttpClient();
        var handle = new HttpApiConnectionHandle(httpClient, "http://localhost", RequestMethod.Get);

        // Act
        var result = handle.GetRawConnection<HttpClient>();

        // Assert
        result.Should().BeSameAs(httpClient);
    }

    [Fact]
    public void GetRawConnection_WithIncorrectType_ThrowsInvalidCastException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var handle = new HttpApiConnectionHandle(httpClient, "http://localhost", RequestMethod.Get);

        // Act
        var act = () => handle.GetRawConnection<string>();

        // Assert
        act.Should().Throw<InvalidCastException>();
    }

    #endregion

    #region 并发锁机制测试

    [Fact]
    public async Task AcquireLockAsync_SingleAcquisition_ReturnsDisposableLock()
    {
        // Arrange
        var httpClient = new HttpClient();
        var handle = new HttpApiConnectionHandle(httpClient, "http://localhost", RequestMethod.Get);

        // Act
        var lockHandle = await handle.AcquireLockAsync(CancellationToken.None);

        // Assert
        lockHandle.Should().NotBeNull();
        lockHandle.Should().BeAssignableTo<IDisposable>();

        // Cleanup
        lockHandle.Dispose();
    }

    [Fact]
    public async Task AcquireLockAsync_ConcurrentAccess_SerializesRequests()
    {
        // Arrange
        var httpClient = new HttpClient();
        var handle = new HttpApiConnectionHandle(httpClient, "http://localhost", RequestMethod.Get);
        var executionOrder = new List<int>();
        var lockAcquired = new TaskCompletionSource<bool>();

        // Act - 启动两个并发任务
        var task1 = Task.Run(async () =>
        {
            using (await handle.AcquireLockAsync(CancellationToken.None))
            {
                executionOrder.Add(1);
                lockAcquired.SetResult(true);
                await Task.Delay(100); // 持有锁一段时间
            }
        });

        // 等待第一个任务获取锁
        await lockAcquired.Task;

        var task2 = Task.Run(async () =>
        {
            using (await handle.AcquireLockAsync(CancellationToken.None))
            {
                executionOrder.Add(2);
            }
        });

        await Task.WhenAll(task1, task2);

        // Assert - 第一个任务应该先完成
        executionOrder.Should().ContainInOrder(1, 2);
    }

    [Fact]
    public async Task AcquireLockAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var handle = new HttpApiConnectionHandle(httpClient, "http://localhost", RequestMethod.Get);
        var cts = new CancellationTokenSource();
        
        // 先获取锁
        var firstLock = await handle.AcquireLockAsync(CancellationToken.None);

        // Act - 尝试获取锁但取消
        var acquireTask = Task.Run(async () =>
        {
            await Task.Delay(50);
            cts.Cancel();
        });

        var act = async () => await handle.AcquireLockAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Cleanup
        firstLock.Dispose();
    }

    [Fact]
    public async Task AcquireLockAsync_LockReleased_AllowsNextAcquisition()
    {
        // Arrange
        var httpClient = new HttpClient();
        var handle = new HttpApiConnectionHandle(httpClient, "http://localhost", RequestMethod.Get);
        var acquiredCount = 0;

        // Act - 获取并释放锁多次
        for (int i = 0; i < 5; i++)
        {
            using (await handle.AcquireLockAsync(CancellationToken.None))
            {
                acquiredCount++;
            }
        }

        // Assert
        acquiredCount.Should().Be(5);
    }

    #endregion

    #region 资源释放测试

    [Fact]
    public async Task DisposeAsync_ReleasesResources()
    {
        // Arrange
        var httpClient = new HttpClient();
        var handle = new HttpApiConnectionHandle(httpClient, "http://localhost", RequestMethod.Get);

        // Act
        await handle.DisposeAsync();

        // Assert - HttpClient should be disposed
        var act = async () => await httpClient.GetAsync("http://localhost");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task DisposeAsync_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var httpClient = new HttpClient();
        var handle = new HttpApiConnectionHandle(httpClient, "http://localhost", RequestMethod.Get);

        // Act
        var act = async () =>
        {
            await handle.DisposeAsync();
            await handle.DisposeAsync();
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region 属性访问测试

    [Fact]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var httpClient = new HttpClient();
        var handle = new HttpApiConnectionHandle(
            httpClient, 
            "http://localhost:5000/api",
            RequestMethod.Post,
            "user",
            "pass",
            "http://gateway");

        // Assert - 验证属性值不能被修改（只读属性）
        handle.AccessApiString.Should().Be("http://localhost:5000/api");
        handle.RequestMethod.Should().Be(RequestMethod.Post);
        handle.Account.Should().Be("user");
        handle.Password.Should().Be("pass");
        handle.Gateway.Should().Be("http://gateway");
    }

    #endregion
}
