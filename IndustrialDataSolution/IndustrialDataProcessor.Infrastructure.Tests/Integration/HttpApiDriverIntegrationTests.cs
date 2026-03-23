using FluentAssertions;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;
using IndustrialDataProcessor.Infrastructure.Communication.Drivers.Api;
using Moq;
using Moq.Protected;
using System.Net;

namespace IndustrialDataProcessor.Infrastructure.Tests.Integration;

/// <summary>
/// API驱动端到端集成测试
/// 模拟真实API调用场景，测试完整的数据采集流程
/// </summary>
public class HttpApiDriverIntegrationTests : IAsyncDisposable
{
    private readonly ConnectionManager _connectionManager;
    private readonly ApiDriver _driver;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly HttpApiInterfaceConfig _defaultProtocolConfig;

    /// <summary>
    /// 模拟工业设备API返回的JSON数据
    /// </summary>
    private const string IndustrialDeviceResponse = """
        {
            "deviceInfo": {
                "id": "DEVICE-001",
                "name": "温度监测站",
                "location": "车间A-区域1"
            },
            "sensors": [
                {
                    "id": "SENSOR-001",
                    "type": "temperature",
                    "value": 25.5,
                    "unit": "°C",
                    "status": "normal",
                    "lastUpdate": "2024-01-15T10:30:00Z"
                },
                {
                    "id": "SENSOR-002",
                    "type": "humidity",
                    "value": 65.2,
                    "unit": "%",
                    "status": "normal",
                    "lastUpdate": "2024-01-15T10:30:00Z"
                },
                {
                    "id": "SENSOR-003",
                    "type": "pressure",
                    "value": 101.325,
                    "unit": "kPa",
                    "status": "warning",
                    "lastUpdate": "2024-01-15T10:30:00Z"
                }
            ],
            "alarms": {
                "active": 1,
                "items": [
                    {
                        "code": "ALM-001",
                        "level": "warning",
                        "message": "压力接近上限",
                        "timestamp": "2024-01-15T10:29:00Z"
                    }
                ]
            },
            "production": {
                "totalOutput": 1500,
                "currentShift": "日班",
                "efficiency": 95.5,
                "quality": {
                    "passRate": 99.2,
                    "defectCount": 12
                }
            },
            "status": "running",
            "uptime": 86400
        }
        """;

    public HttpApiDriverIntegrationTests()
    {
        _connectionManager = new ConnectionManager();
        _driver = new ApiDriver();
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object);
        _defaultProtocolConfig = new HttpApiInterfaceConfig
        {
            Id = "TEST-API-001",
            AccessApiString = "http://localhost:5000/api/device/001",
            RequestMethod = RequestMethod.Get
        };
    }

    public async ValueTask DisposeAsync()
    {
        _driver.Dispose();
        _httpClient.Dispose();
        await _connectionManager.DisposeAsync();
    }

    private void SetupMockResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
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

    #region 完整采集流程测试

    [Fact]
    public async Task FullCollectionFlow_WithMultipleParameters_CollectsAllValues()
    {
        // Arrange
        SetupMockResponse(IndustrialDeviceResponse);
        
        var handle = new HttpApiConnectionHandle(
            _httpClient,
            "http://localhost:5000/api/device/001",
            RequestMethod.Get);

        // 创建多个参数配置，模拟ParameterConfig中的Address字段
        var parameters = new List<ParameterConfig>
        {
            new() { Label = "设备名称", Address = "deviceInfo.name", DataType = DataType.String },
            new() { Label = "温度值", Address = "sensors[0].value", DataType = DataType.Float },
            new() { Label = "湿度值", Address = "sensors[1].value", DataType = DataType.Float },
            new() { Label = "压力值", Address = "sensors[2].value", DataType = DataType.Float },
            new() { Label = "生产总量", Address = "production.totalOutput", DataType = DataType.Int },
            new() { Label = "效率", Address = "production.efficiency", DataType = DataType.Float },
            new() { Label = "合格率", Address = "production.quality.passRate", DataType = DataType.Float },
            new() { Label = "运行状态", Address = "status", DataType = DataType.String },
            new() { Label = "运行时间", Address = "uptime", DataType = DataType.Int },
            new() { Label = "报警数量", Address = "alarms.active", DataType = DataType.Int }
        };

        // Act
        var results = new List<(string Label, object? Value, bool Success)>();
        foreach (var param in parameters)
        {
            var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", param, CancellationToken.None);
            results.Add((result!.Label, result.Value, result.ReadIsSuccess));
        }

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.Success);

        // 验证具体值
        results.First(r => r.Label == "设备名称").Value.Should().Be("温度监测站");
        results.First(r => r.Label == "温度值").Value.Should().Be(25.5);
        results.First(r => r.Label == "湿度值").Value.Should().Be(65.2);
        results.First(r => r.Label == "压力值").Value.Should().Be(101.325);
        results.First(r => r.Label == "生产总量").Value.Should().Be(1500);
        results.First(r => r.Label == "效率").Value.Should().Be(95.5);
        results.First(r => r.Label == "合格率").Value.Should().Be(99.2);
        results.First(r => r.Label == "运行状态").Value.Should().Be("running");
        results.First(r => r.Label == "运行时间").Value.Should().Be(86400);
        results.First(r => r.Label == "报警数量").Value.Should().Be(1);
    }

    [Fact]
    public async Task FullCollectionFlow_WithConnectionManager_WorksEndToEnd()
    {
        // Arrange - 这个测试不使用Mock，直接测试ConnectionManager创建的连接
        // 注意：这只是验证创建流程，实际HTTP请求会失败
        var apiConfig = new HttpApiInterfaceConfig
        {
            Id = "INTEGRATION-001",
            AccessApiString = "http://localhost:9999/api/test", // 使用不存在的端口
            RequestMethod = RequestMethod.Get,
            ReceiveTimeOut = 1000 // 短超时
        };

        // Act
        var handle = await _connectionManager.GetOrCreateConnectionAsync(apiConfig, CancellationToken.None);

        // Assert - 验证连接句柄创建成功
        handle.Should().NotBeNull();
        handle.Should().BeOfType<HttpApiConnectionHandle>();
        
        var apiHandle = handle as HttpApiConnectionHandle;
        apiHandle!.AccessApiString.Should().Be("http://localhost:9999/api/test");
    }

    #endregion

    #region 数组数据采集测试

    [Fact]
    public async Task ArrayDataCollection_ExtractsMultipleArrayElements()
    {
        // Arrange
        SetupMockResponse(IndustrialDeviceResponse);
        
        var handle = new HttpApiConnectionHandle(
            _httpClient,
            "http://localhost:5000/api/device/001",
            RequestMethod.Get);

        var parameters = new List<ParameterConfig>
        {
            new() { Label = "传感器1类型", Address = "sensors[0].type", DataType = DataType.String },
            new() { Label = "传感器1值", Address = "sensors[0].value", DataType = DataType.Float },
            new() { Label = "传感器2类型", Address = "sensors[1].type", DataType = DataType.String },
            new() { Label = "传感器2值", Address = "sensors[1].value", DataType = DataType.Float },
            new() { Label = "传感器3类型", Address = "sensors[2].type", DataType = DataType.String },
            new() { Label = "传感器3值", Address = "sensors[2].value", DataType = DataType.Float }
        };

        // Act
        var results = new Dictionary<string, object?>();
        foreach (var param in parameters)
        {
            var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", param, CancellationToken.None);
            if (result != null && result.ReadIsSuccess)
                results[result.Label] = result.Value;
        }

        // Assert
        results["传感器1类型"].Should().Be("temperature");
        results["传感器1值"].Should().Be(25.5);
        results["传感器2类型"].Should().Be("humidity");
        results["传感器2值"].Should().Be(65.2);
        results["传感器3类型"].Should().Be("pressure");
        results["传感器3值"].Should().Be(101.325);
    }

    #endregion

    #region 并发访问安全测试

    [Fact]
    public async Task ConcurrentAccess_MultipleThreads_ThreadSafe()
    {
        // Arrange
        SetupMockResponse(IndustrialDeviceResponse);
        
        var handle = new HttpApiConnectionHandle(
            _httpClient,
            "http://localhost:5000/api/device/001",
            RequestMethod.Get);

        var parameter = new ParameterConfig
        {
            Label = "温度值",
            Address = "sensors[0].value",
            DataType = DataType.Float
        };

        // Act - 启动多个并发任务
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - 所有结果应该成功且值相同
        results.Should().OnlyContain(r => r!.ReadIsSuccess);
        results.Should().OnlyContain(r => (double)r!.Value! == 25.5);
    }

    [Fact]
    public async Task ConcurrentAccess_DifferentParameters_AllSucceed()
    {
        // Arrange
        SetupMockResponse(IndustrialDeviceResponse);
        
        var handle = new HttpApiConnectionHandle(
            _httpClient,
            "http://localhost:5000/api/device/001",
            RequestMethod.Get);

        var parameters = new[]
        {
            new ParameterConfig { Label = "温度", Address = "sensors[0].value", DataType = DataType.Float },
            new ParameterConfig { Label = "湿度", Address = "sensors[1].value", DataType = DataType.Float },
            new ParameterConfig { Label = "压力", Address = "sensors[2].value", DataType = DataType.Float },
            new ParameterConfig { Label = "状态", Address = "status", DataType = DataType.String },
            new ParameterConfig { Label = "产量", Address = "production.totalOutput", DataType = DataType.Int }
        };

        // Act - 并发读取不同参数
        var tasks = parameters.Select(p => _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", p, CancellationToken.None));
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(5);
        results.Should().OnlyContain(r => r!.ReadIsSuccess);
    }

    #endregion

    #region 缓存一致性测试

    [Fact]
    public async Task CacheConsistency_SameRequestCycle_UsesCache()
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
                Interlocked.Increment(ref callCount);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(IndustrialDeviceResponse, System.Text.Encoding.UTF8, "application/json")
                };
            });

        var handle = new HttpApiConnectionHandle(
            _httpClient,
            "http://localhost:5000/api/device/001",
            RequestMethod.Get);

        // 模拟一个采集周期中读取多个参数
        var parameters = Enumerable.Range(0, 50)
            .Select(i => new ParameterConfig
            {
                Label = $"参数{i}",
                Address = i % 3 == 0 ? "sensors[0].value" : 
                         i % 3 == 1 ? "sensors[1].value" : "sensors[2].value",
                DataType = DataType.Float
            })
            .ToList();

        // Act
        foreach (var param in parameters)
        {
            await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", param, CancellationToken.None);
        }

        // Assert - 由于缓存，HTTP请求应该只发送一次
        callCount.Should().Be(1);
    }

    #endregion

    #region ParameterConfig Address字段测试

    [Fact]
    public async Task ParameterConfigAddress_AsJsonPath_ExtractsCorrectValue()
    {
        // Arrange
        SetupMockResponse(IndustrialDeviceResponse);
        
        var handle = new HttpApiConnectionHandle(
            _httpClient,
            "http://localhost:5000/api/device/001",
            RequestMethod.Get);

        // 这是用户需求中描述的场景：
        // ParameterConfig 中包含一个 Address 字段，该字段的值是一个点号表示法描述的JSON路径
        var parameterConfigs = new List<ParameterConfig>
        {
            // 基本属性路径
            new() { Label = "DeviceName", Address = "deviceInfo.name", DataType = DataType.String },
            
            // 数组元素路径
            new() { Label = "FirstSensorValue", Address = "sensors[0].value", DataType = DataType.Float },
            
            // 深层嵌套路径
            new() { Label = "QualityPassRate", Address = "production.quality.passRate", DataType = DataType.Float },
            
            // 报警信息路径
            new() { Label = "AlarmMessage", Address = "alarms.items[0].message", DataType = DataType.String }
        };

        // Act
        var collectedData = new Dictionary<string, object?>();
        foreach (var config in parameterConfigs)
        {
            var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", config, CancellationToken.None);
            
            // Label是key，Address路径提取的值是这个Label的值
            if (result != null && result.ReadIsSuccess)
            {
                collectedData[result.Label] = result.Value;
            }
        }

        // Assert - 验证Address作为JSON路径正确提取了值
        collectedData["DeviceName"].Should().Be("温度监测站");
        collectedData["FirstSensorValue"].Should().Be(25.5);
        collectedData["QualityPassRate"].Should().Be(99.2);
        collectedData["AlarmMessage"].Should().Be("压力接近上限");
    }

    [Fact]
    public async Task ParameterConfigAddress_WithInvalidPath_HandlesGracefully()
    {
        // Arrange
        SetupMockResponse(IndustrialDeviceResponse);
        
        var handle = new HttpApiConnectionHandle(
            _httpClient,
            "http://localhost:5000/api/device/001",
            RequestMethod.Get);

        var invalidConfigs = new List<ParameterConfig>
        {
            new() { Label = "Invalid1", Address = "nonexistent.path", DataType = DataType.String },
            new() { Label = "Invalid2", Address = "sensors[99].value", DataType = DataType.Float },
            new() { Label = "Invalid3", Address = "deviceInfo.nonexistent", DataType = DataType.String }
        };

        // Act
        var results = new List<(string Label, bool Success, string? Error)>();
        foreach (var config in invalidConfigs)
        {
            var result = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", config, CancellationToken.None);
            results.Add((result!.Label, result.ReadIsSuccess, result.ErrorMsg));
        }

        // Assert - 无效路径应该返回失败但不抛出异常
        results.Should().OnlyContain(r => !r.Success);
        results.Should().OnlyContain(r => r.Error != null);
    }

    #endregion

    #region 不同数据类型测试

    [Fact]
    public async Task DataTypeExtraction_AllTypes_ExtractsCorrectly()
    {
        // Arrange
        SetupMockResponse(IndustrialDeviceResponse);
        
        var handle = new HttpApiConnectionHandle(
            _httpClient,
            "http://localhost:5000/api/device/001",
            RequestMethod.Get);

        // Act & Assert - 字符串类型
        var stringResult = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", 
            new ParameterConfig { Label = "名称", Address = "deviceInfo.name", DataType = DataType.String },
            CancellationToken.None);
        stringResult!.Value.Should().BeOfType<string>();
        stringResult.Value.Should().Be("温度监测站");

        // Act & Assert - 浮点类型
        var floatResult = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001",
            new ParameterConfig { Label = "温度", Address = "sensors[0].value", DataType = DataType.Float },
            CancellationToken.None);
        floatResult!.Value.Should().BeOfType<double>();

        // Act & Assert - 整数类型
        var intResult = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001",
            new ParameterConfig { Label = "产量", Address = "production.totalOutput", DataType = DataType.Int },
            CancellationToken.None);
        intResult!.Value.Should().BeOfType<int>();
        intResult.Value.Should().Be(1500);

        // Act & Assert - 布尔类型（通过字符串"warning"等状态）
        var statusResult = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001",
            new ParameterConfig { Label = "状态", Address = "sensors[2].status", DataType = DataType.String },
            CancellationToken.None);
        statusResult!.Value.Should().Be("warning");
    }

    #endregion

    #region 错误恢复测试

    [Fact]
    public async Task ErrorRecovery_AfterFailure_CanRetry()
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
                // 第一次调用失败，第二次成功
                if (callCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = new StringContent("Error")
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(IndustrialDeviceResponse, System.Text.Encoding.UTF8, "application/json")
                };
            });

        var handle = new HttpApiConnectionHandle(
            _httpClient,
            "http://localhost:5000/api/device/001",
            RequestMethod.Get);

        var parameter = new ParameterConfig
        {
            Label = "温度",
            Address = "sensors[0].value",
            DataType = DataType.Float
        };

        // Act
        var firstResult = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);
        
        // 清除缓存以允许重试
        _driver.ClearAllCache();
        
        var secondResult = await _driver.ReadAsync(handle, _defaultProtocolConfig, "TEST-EQUIP-001", parameter, CancellationToken.None);

        // Assert
        firstResult!.ReadIsSuccess.Should().BeFalse();
        secondResult!.ReadIsSuccess.Should().BeTrue();
        secondResult.Value.Should().Be(25.5);
    }

    #endregion
}
