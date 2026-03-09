//using FluentAssertions;
//using HslCommunication.ModBus;
//using IndustrialDataProcessor.Domain.Enums;
//using IndustrialDataProcessor.Domain.Workstation.Configs;
//using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
//using IndustrialDataProcessor.Domain.Workstation.Results;
//using IndustrialDataProcessor.Infrastructure.Communication.Connection;
//using IndustrialDataProcessor.Infrastructure.Communication.Drivers;

//namespace IndustrialDataProcessor.Infrastructure.Tests.Integration;

//[Trait("Integration", "ModbusTcpDriverIntegrationTests")]
//public class ModbusTcpDriverIntegrationTests : IAsyncLifetime
//{
//    private ModbusTcpServer _modbus = null!;
//    private ConnectionManager _connectionManager = null!;
//    private ModbusTcpNetDriver _driver = null!;

//    private readonly int _testPort = 5020; // 避开默认的 502 端口，防止冲突
//    private readonly string _testIp = "127.0.0.1";

//    async ValueTask IAsyncLifetime.InitializeAsync()
//    {
//        // 启动 HSL 的虚拟 Modbus 服务器
//        _modbus = new ModbusTcpServer();
//        _modbus.Station = 1;
//        _modbus.DataFormat = HslCommunication.Core.DataFormat.CDAB;
//        _modbus.ServerStart(_testPort);

//        // 预置一些测试数据 (例如给 40001 寄存器写入一个 Short 值 1234)
//        _modbus.Write("40001", (short)1234);
//        _modbus.Write("40002", 56.78f); // 写入一个 Float

//        _connectionManager = new ConnectionManager();
//        _driver = new ModbusTcpNetDriver();
//    }

//    async ValueTask IAsyncDisposable.DisposeAsync()
//    {
//        await _connectionManager.DisposeAsync();
//        _modbus?.ServerClose();
//    }

//    [Fact(DisplayName = "连接管理器 - 应该复用同一个IP和端口的连接")]
//    public async Task ConnectionManager_Should_Reuse_Connection()
//    {
//        // Arrange
//        var config = new NetworkProtocolConfig
//        {
//            Id = "Protocol-001",
//            IpAddress = _testIp,
//            ProtocolPort = _testPort
//        };

//        // Act
//        var handle1 = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);
//        var handle2 = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);

//        // Assert
//        handle1.Should().NotBeNull();
//        handle1.Should().BeSameAs(handle2, "因为配置ID相同，应该返回缓存中的同一个连接句柄");
//    }

//    [Fact(DisplayName = "驱动读取 - 应该成功从真实的TCP服务端读取数据")]
//    public async Task Driver_Should_Read_Data_Correctly()
//    {
//        // Arrange
//        var config = new NetworkProtocolConfig { Id = "P-01", IpAddress = _testIp, ProtocolPort = _testPort };
//        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);

//        var pointShort = new ParameterConfig { Address = "40001", DataType = DataType.Short, StationNo = "1" };
//        var pointFloat = new ParameterConfig { Address = "40002", DataType = DataType.Float, StationNo = "1" };

//        // Act
//        var resultShort = await _driver.ReadAsync(handle, config, "Eq-1", pointShort, CancellationToken.None);
//        var resultFloat = await _driver.ReadAsync(handle, config, "Eq-1", pointFloat, CancellationToken.None);

//        // Assert
//        resultShort.Should().NotBeNull();
//        resultShort!.ReadIsSuccess.Should().BeTrue();
//        resultShort.Value.Should().Be((short)1234);

//        resultFloat.Should().NotBeNull();
//        resultFloat!.ReadIsSuccess.Should().BeTrue();
//        // Float 比较允许极小的精度误差
//        ((float)resultFloat.Value!).Should().BeApproximately(56.78f, 0.001f);
//    }

//    [Fact(DisplayName = "并发测试 - 多个线程同时读取时，底层锁应该保证不报错且数据正确")]
//    public async Task Driver_Should_Handle_Concurrency_Safely()
//    {
//        // Arrange
//        var config = new NetworkProtocolConfig { Id = "P-01", IpAddress = _testIp, ProtocolPort = _testPort };
//        var handle = await _connectionManager.GetOrCreateConnectionAsync(config, CancellationToken.None);
//        var point = new ParameterConfig { Address = "40001", DataType = DataType.Short, StationNo = "1" };

//        int concurrentCount = 50;
//        var tasks = new List<Task<PointResult?>>();

//        // Act: 瞬间发起 50 个并发读取请求
//        for (int i = 0; i < concurrentCount; i++)
//        {
//            tasks.Add(Task.Run(() => _driver.ReadAsync(handle, config, "Eq-1", point, CancellationToken.None)));
//        }

//        var results = await Task.WhenAll(tasks);

//        // Assert
//        results.Should().HaveCount(concurrentCount);
//        foreach (var result in results)
//        {
//            result.Should().NotBeNull();
//            result!.ReadIsSuccess.Should().BeTrue("并发锁应该保证所有请求排队执行，而不是互相干扰导致TCP报文错乱");
//            result.Value.Should().Be((short)1234);
//        }
//    }
//}
