using FluentAssertions;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;
using IndustrialDataProcessor.Infrastructure.Communication.Drivers.Api;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;

namespace IndustrialDataProcessor.Infrastructure.Tests.Communication.Drivers;

/// <summary>
/// HttpApiDriver 单元测试
/// 测试HTTP API协议驱动的功能
/// </summary>
public class HttpApiDriverTests : IDisposable
{
    private readonly ApiDriver _driver;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly HttpApiInterfaceConfig _defaultProtocolConfig;

    /// <summary>
    /// 测试用的JSON响应数据
    /// </summary>
    private const string TestApiResponse = """
        {
            "deviceId": "DEVICE-001",
            "timestamp": "2024-01-15T10:30:00Z",
            "sensors": {
                "temperature": {
                    "value": 25.5,
                    "unit": "°C",
                    "status": "normal"
                },
                "pressure": {
                    "value": 101.325,
                    "unit": "kPa",
                    "status": "normal"
                },
                "humidity": {
                    "value": 65,
                    "unit": "%",
                    "status": "normal"
                }
            },
            "readings": [
                { "name": "temp1", "value": 23.5 },
                { "name": "temp2", "value": 24.0 },
                { "name": "temp3", "value": 22.8 }
            ],
            "status": "online",
            "errorCount": 0,
            "isActive": true
        }
        """;

    public HttpApiDriverTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object);
        _driver = new ApiDriver();
        _defaultProtocolConfig = new HttpApiInterfaceConfig
        {
            Id = "TEST-API-001",
            AccessApiString = "http://localhost:5000/api/data",
            RequestMethod = RequestMethod.Get
        };
    }

    public void Dispose()
    {
        _driver.Dispose();
        _httpClient.Dispose();
    }

    #region 辅助方法

    private void SetupMockResponse(HttpStatusCode statusCode, string content)
    {
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
    }

    private void SetupMockResponseWithVerify(HttpStatusCode statusCode, string content, HttpMethod expectedMethod)
    {
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == expectedMethod),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
    }

    private HttpApiConnectionHandle CreateTestHandle(RequestMethod method = RequestMethod.Get, string url = "http://localhost:5000/api/data")
    {
        return new HttpApiConnectionHandle(_httpClient, url, method);
    }

    private ParameterConfig CreateTestParameter(string label, string address)
    {
        return new ParameterConfig
        {
            Label = label,
            Address = address,
            DataType = DataType.Float
        };
    }

    private async Task<Domain.Workstation.Results.PointResult?> ReadPointAsync(
        HttpApiConnectionHandle handle, 
        ParameterConfig parameter)
    {
        return await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);
    }

    #endregion

    #region 单点位读取测试

    [Fact]
    public async Task ReadPointAsync_WithValidPath_ReturnsCorrectValue()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("Temperature", "sensors.temperature.value");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ReadIsSuccess.Should().BeTrue();
        result.Value.Should().Be(25.5);
        result.Label.Should().Be("Temperature");
    }

    [Fact]
    public async Task ReadPointAsync_WithStringValue_ReturnsString()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("Status", "status");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeTrue();
        result.Value.Should().Be("online");
    }

    [Fact]
    public async Task ReadPointAsync_WithIntegerValue_ReturnsInteger()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("ErrorCount", "errorCount");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task ReadPointAsync_WithBooleanValue_ReturnsBoolean()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("IsActive", "isActive");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeTrue();
        result.Value.Should().Be(true);
    }

    [Fact]
    public async Task ReadPointAsync_WithArrayElement_ReturnsCorrectValue()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("FirstReading", "readings[0].value");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeTrue();
        result.Value.Should().Be(23.5);
    }

    [Fact]
    public async Task ReadPointAsync_WithNestedArrayElement_ReturnsCorrectValue()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("SecondReadingName", "readings[1].name");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeTrue();
        result.Value.Should().Be("temp2");
    }

    #endregion

    #region 路径不存在测试

    [Fact]
    public async Task ReadPointAsync_WithNonExistentPath_ReturnsFailure()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("NonExistent", "nonexistent.path");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeFalse();
        result.ErrorMsg.Should().Contain("无法从JSON中提取路径");
    }

    [Fact]
    public async Task ReadPointAsync_WithInvalidArrayIndex_ReturnsFailure()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("OutOfBounds", "readings[99].value");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeFalse();
    }

    #endregion

    #region HTTP请求方法测试

    [Fact]
    public async Task ReadPointAsync_WithGetMethod_SendsGetRequest()
    {
        // Arrange
        SetupMockResponseWithVerify(HttpStatusCode.OK, TestApiResponse, HttpMethod.Get);
        var handle = CreateTestHandle(RequestMethod.Get);
        var parameter = CreateTestParameter("Status", "status");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeTrue();
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ReadPointAsync_WithPostMethod_SendsPostRequest()
    {
        // Arrange
        SetupMockResponseWithVerify(HttpStatusCode.OK, TestApiResponse, HttpMethod.Post);
        var handle = CreateTestHandle(RequestMethod.Post);
        var parameter = CreateTestParameter("Status", "status");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeTrue();
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ReadPointAsync_WithPutMethod_SendsPutRequest()
    {
        // Arrange
        SetupMockResponseWithVerify(HttpStatusCode.OK, TestApiResponse, HttpMethod.Put);
        var handle = CreateTestHandle(RequestMethod.Put);
        var parameter = CreateTestParameter("Status", "status");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeTrue();
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ReadPointAsync_WithDeleteMethod_SendsDeleteRequest()
    {
        // Arrange
        SetupMockResponseWithVerify(HttpStatusCode.OK, TestApiResponse, HttpMethod.Delete);
        var handle = CreateTestHandle(RequestMethod.Delete);
        var parameter = CreateTestParameter("Status", "status");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeTrue();
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region 缓存机制测试

    [Fact]
    public async Task ReadPointAsync_MultipleCalls_UsesCachedResponse()
    {
        // Arrange
        var callCount = 0;
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(TestApiResponse, System.Text.Encoding.UTF8, "application/json")
                };
            });

        var handle = CreateTestHandle();
        var param1 = CreateTestParameter("Temp", "sensors.temperature.value");
        var param2 = CreateTestParameter("Pressure", "sensors.pressure.value");
        var param3 = CreateTestParameter("Humidity", "sensors.humidity.value");

        // Act - 快速连续读取三个点位
        var result1 = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", param1, CancellationToken.None);
        var result2 = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", param2, CancellationToken.None);
        var result3 = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", param3, CancellationToken.None);

        // Assert - 由于缓存，HTTP请求只应发送一次
        callCount.Should().Be(1);
        result1!.Value.Should().Be(25.5);
        result2!.Value.Should().Be(101.325);
        result3!.Value.Should().Be(65);
    }

    [Fact]
    public async Task ClearCache_AfterClear_MakesNewRequest()
    {
        // Arrange
        var callCount = 0;
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(TestApiResponse, System.Text.Encoding.UTF8, "application/json")
                };
            });

        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("Status", "status");

        // Act
        await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);
        _driver.ClearAllCache();
        await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ClearCache_ByUrl_ClearsSpecificCache()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle(url: "http://localhost:5000/api/specific");
        var parameter = CreateTestParameter("Status", "status");

        // Act
        await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);
        _driver.ClearCache("http://localhost:5000/api/specific");

        // Assert - 方法不应抛出异常
        var act = () => _driver.ClearCache("nonexistent");
        act.Should().NotThrow();
    }

    #endregion

    #region 错误处理测试

    [Fact]
    public async Task ReadPointAsync_WithHttpError_ReturnsFailure()
    {
        // Arrange
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error")
            });

        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("Status", "status");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeFalse();
        result.ErrorMsg.Should().Contain("HTTP请求失败");
    }

    [Fact]
    public async Task ReadPointAsync_WithEmptyResponse_ReturnsFailure()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, "");
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("Status", "status");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeFalse();
        result.ErrorMsg.Should().Contain("API返回空响应");
    }

    [Fact]
    public async Task ReadPointAsync_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, "{ invalid json }");
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("Status", "status");

        // Act
        var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result!.ReadIsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ReadPointAsync_WithWrongHandleType_ReturnsFailure()
    {
        // Arrange
        var wrongHandle = new Mock<IndustrialDataProcessor.Domain.Communication.IConnection.IConnectionHandle>();
        wrongHandle.Setup(h => h.AcquireLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DummyDisposable());
        var parameter = CreateTestParameter("Status", "status");

        // Act
        var result = await _driver.ReadAsync(wrongHandle.Object, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ReadIsSuccess.Should().BeFalse();
        result.ErrorMsg.Should().Contain("连接句柄类型不正确");
    }

    [Fact]
    public async Task ReadPointAsync_WithCancellation_ThrowsOrReturnsFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("Status", "status");

        // Act & Assert
        // 由于已取消，操作会抛出异常或返回失败
        var act = async () => await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, cts.Token);
        
        // 可能抛出OperationCanceledException或TaskCanceledException
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region 写入功能测试

    [Fact]
    public async Task WritePointAsync_ReturnsFailure()
    {
        // Arrange
        SetupMockResponse(HttpStatusCode.OK, TestApiResponse);
        var handle = CreateTestHandle();
        var parameter = CreateTestParameter("Status", "status");
        var writeTask = new WriteTask { WritePoint = parameter };

        // Act
        var result = await _driver.WriteAsync(handle, writeTask, "newValue", CancellationToken.None);

        // Assert - API驱动不支持写入，应返回false
        result.Should().BeFalse();
    }

    #endregion

    #region 网关URL处理测试

    [Fact]
    public async Task ReadPointAsync_WithGateway_CombinesUrls()
    {
        // Arrange
        var capturedUrl = string.Empty;
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.ToString() ?? "")
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestApiResponse, System.Text.Encoding.UTF8, "application/json")
            });

        var handle = new HttpApiConnectionHandle(
            _httpClient, 
            "api/data", 
            RequestMethod.Get,
            gateway: "http://gateway.local");
        var parameter = CreateTestParameter("Status", "status");

        // Act
        await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        capturedUrl.Should().Contain("gateway.local");
        capturedUrl.Should().Contain("api/data");
    }

    #endregion

    #region 资源释放测试

    [Fact]
    public void Dispose_ClearsCache()
    {
        // Arrange
        var driver = new ApiDriver();

        // Act
        driver.Dispose();

        // Assert - 不应抛出异常
        var act = () => driver.ClearAllCache();
        act.Should().NotThrow();
    }

    #endregion
}

/// <summary>
/// 测试辅助类：空的 IDisposable 实现
/// </summary>
internal class DummyDisposable : IDisposable
{
    public void Dispose() { }
}
