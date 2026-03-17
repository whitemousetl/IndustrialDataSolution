using FluentAssertions;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using IndustrialDataProcessor.Domain.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IndustrialDataProcessor.Api.Tests.Integration;

/// <summary>
/// 工作站配置 API 集成测试
/// </summary>
[Trait("Integration", "WorkstationConfigApiTests")]
public class WorkstationConfigApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IWorkstationConfigPersistenceRepository> _mockRepository;

    public WorkstationConfigApiTests(WebApplicationFactory<Program> factory)
    {
        _mockRepository = new Mock<IWorkstationConfigPersistenceRepository>();
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<IndustrialDataProcessor.Domain.Entities.WorkstationConfigEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 替换真实的仓储为 Mock 仓储
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IWorkstationConfigPersistenceRepository));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(_mockRepository.Object);
            });
        });
        _client = _factory.CreateClient();
    }

    #region 辅助方法

    /// <summary>
    /// 发送 POST 请求并解析响应
    /// </summary>
    private async Task<(HttpStatusCode StatusCode, JsonElement Content)> PostConfigAsync(object request)
    {
        var response = await _client.PostAsJsonAsync("/api/workstation-config", request);
        JsonElement content = default;
        if (response.Content.Headers.ContentLength > 0)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            content = result;
        }
        return (response.StatusCode, content);
    }

    #endregion

    #region 成功场景测试

    [Fact(DisplayName = "TC-SUCCESS-01: LAN接口-ModbusTcpNet完整配置")]
    public async Task SaveConfig_LanModbusTcpNetFullConfig_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Id = "WS-001",
            Name = "工作站1",
            IpAddress = "192.168.1.100",
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 0,  // ModbusTcpNet = 0
                    IpAddress = "192.168.1.200",
                    ProtocolPort = 502,
                    CommunicationDelay = 50000,
                    ReceiveTimeOut = 10000,
                    ConnectTimeOut = 10000,
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            IsCollect = true,
                            Name = "设备1",
                            EquipmentType = 0, // Equipment = 0
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "温度",
                                    Address = "40001",
                                    IsMonitor = true,
                                    StationNo = "1",
                                    DataFormat = 0, // ABCD = 0
                                    DataType = 7,   // Float = 7
                                    AddressStartWithZero = true,
                                    Length = 2,
                                    Cycle = 1000
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
        content.GetProperty("code").GetInt32().Should().Be(200);
    }

    [Fact(DisplayName = "TC-SUCCESS-02: COM接口-ModbusRtu完整配置")]
    public async Task SaveConfig_ComModbusRtuFullConfig_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-002",
                    InterfaceType = 1, // COM = 1
                    ProtocolType = 100, // ModbusRtu = 100
                    SerialPortName = "COM1",
                    BaudRate = 9600,   // B9600 = 9600
                    DataBits = 8,
                    Parity = 0,        // None = 0
                    StopBits = 1,      // One = 1
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            EquipmentType = 0, // Equipment = 0
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "压力",
                                    Address = "40001",
                                    StationNo = "1",
                                    DataFormat = 0, // ABCD = 0
                                    DataType = 7,   // Float = 7
                                    AddressStartWithZero = true
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "TC-SUCCESS-03: DATABASE接口-MySQL完整配置")]
    public async Task SaveConfig_DatabaseMySQLFullConfig_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-003",
                    InterfaceType = 3, // DATABASE = 3
                    ProtocolType = 300, // MySQL = 300
                    DatabaseName = "industrial_db",
                    DatabaseConnectString = "Server=localhost;Database=industrial_db;Uid=root;Pwd=123456;",
                    QuerySqlString = "SELECT * FROM sensor_data",
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            EquipmentType = 0, // Equipment = 0
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "数据值",
                                    Address = "column_name"
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "TC-SUCCESS-04: API接口完整配置")]
    public async Task SaveConfig_ApiFullConfig_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-004",
                    InterfaceType = 2, // API = 2
                    ProtocolType = 200, // Api = 200
                    RequestMethod = 0,  // Get = 0
                    AccessApiString = "https://api.example.com/data",
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            EquipmentType = 0, // Equipment = 0
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "API数据",
                                    Address = "$.data.value"
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "TC-SUCCESS-05: OpcUa最简配置-只需Label和Address")]
    public async Task SaveConfig_OpcUaMinimalConfig_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-005",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 14, // OpcUa = 14
                    IpAddress = "192.168.1.200",
                    ProtocolPort = 4840,
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            EquipmentType = 0, // Equipment = 0
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "OPC节点",
                                    Address = "ns=2;s=Temperature"
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "TC-SUCCESS-06: 设备无参数配置")]
    public async Task SaveConfig_EquipmentNoParameters_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-006",
                    InterfaceType = 2, // API = 2
                    ProtocolType = 200, // Api = 200
                    RequestMethod = 0,  // Get = 0
                    AccessApiString = "https://api.example.com/data",
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            EquipmentType = 0, // Equipment = 0
                            Parameters = new object[] { }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "TC-SUCCESS-07: CJT188协议完整配置")]
    public async Task SaveConfig_CJT188FullConfig_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-007",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 11, // CJT1882004OverTcp = 11
                    IpAddress = "192.168.1.200",
                    ProtocolPort = 502,
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            EquipmentType = 1, // Instrument = 1
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "水表读数",
                                    Address = "0001",
                                    StationNo = "1122334455667788",
                                    DataType = 7, // Float = 7
                                    InstrumentType = 16 // ColdWater = 16
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "TC-SUCCESS-08: 西门子S1200配置")]
    public async Task SaveConfig_SiemensS1200_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-008",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 5,  // SiemensS1200 = 5
                    IpAddress = "192.168.1.200",
                    ProtocolPort = 102,
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "DB块数据",
                                    Address = "DB1.DBD0",
                                    DataType = 7 // Float = 7
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "TC-SUCCESS-09: 欧姆龙FinsTcp配置")]
    public async Task SaveConfig_OmronFinsTcp_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-009",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 2,  // OmronFinsTcp = 2
                    IpAddress = "192.168.1.200",
                    ProtocolPort = 9600,
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "DM区数据",
                                    Address = "D100",
                                    DataType = 7 // Float = 7
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region 验证失败场景测试

    [Fact(DisplayName = "TC-WS-01: Protocols为null")]
    public async Task SaveConfig_ProtocolsNull_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Id = "WS-001",
            Protocols = (object?)null
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
    }

    [Fact(DisplayName = "TC-WS-02: Protocols为空数组")]
    public async Task SaveConfig_ProtocolsEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Id = "WS-001",
            Protocols = new object[] { }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().NotBeNull();
    }

    [Fact(DisplayName = "TC-P-01: 协议Id为空")]
    public async Task SaveConfig_ProtocolIdEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "",
                    InterfaceType = 2, // API = 2
                    ProtocolType = 200, // Api = 200
                    RequestMethod = 0, // Get = 0
                    AccessApiString = "https://api.example.com",
                    Equipments = new[] { new { Id = "E-001", Parameters = new object[] { } } }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-P-LAN-01: LAN接口-缺少IP地址")]
    public async Task SaveConfig_LanMissingIpAddress_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 0,  // ModbusTcpNet = 0
                    ProtocolPort = 502,
                    Equipments = new[] { new { Id = "E-001", Parameters = new object[] { } } }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-P-LAN-02: LAN接口-IP地址格式错误")]
    public async Task SaveConfig_LanInvalidIpAddress_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 0,  // ModbusTcpNet = 0
                    IpAddress = "invalid-ip-address",
                    ProtocolPort = 502,
                    Equipments = new[] { new { Id = "E-001", Parameters = new object[] { } } }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-P-LAN-04: LAN接口-端口为0")]
    public async Task SaveConfig_LanPortZero_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 0,  // ModbusTcpNet = 0
                    IpAddress = "192.168.1.200",
                    ProtocolPort = 0,
                    Equipments = new[] { new { Id = "E-001", Parameters = new object[] { } } }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-P-COM-01: COM接口-缺少串口名称")]
    public async Task SaveConfig_ComMissingSerialPortName_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 1, // COM = 1
                    ProtocolType = 100, // ModbusRtu = 100
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = 0, // None = 0
                    StopBits = 1, // One = 1
                    Equipments = new[] { new { Id = "E-001", Parameters = new object[] { } } }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-P-DB-01: DATABASE接口-缺少数据库名称")]
    public async Task SaveConfig_DatabaseMissingName_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 3, // DATABASE = 3
                    ProtocolType = 300, // MySQL = 300
                    DatabaseConnectString = "Server=localhost;",
                    QuerySqlString = "SELECT * FROM t",
                    Equipments = new[] { new { Id = "E-001", Parameters = new object[] { } } }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-P-API-01: API接口-缺少请求方法")]
    public async Task SaveConfig_ApiMissingRequestMethod_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 2, // API = 2
                    ProtocolType = 200, // Api = 200
                    AccessApiString = "https://api.example.com",
                    Equipments = new[] { new { Id = "E-001", Parameters = new object[] { } } }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-E-01: 设备Id为空")]
    public async Task SaveConfig_EquipmentIdEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 2, // API = 2
                    ProtocolType = 200, // Api = 200
                    RequestMethod = 0, // Get = 0
                    AccessApiString = "https://api.example.com",
                    Equipments = new[] { new { Id = "", Parameters = new object[] { } } }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-PM-01: 参数Label为空")]
    public async Task SaveConfig_ParameterLabelEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 2, // API = 2
                    ProtocolType = 200, // Api = 200
                    RequestMethod = 0, // Get = 0
                    AccessApiString = "https://api.example.com",
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            Parameters = new[]
                            {
                                new { Label = "", Address = "$.value" }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-PM-02: 参数Address为空")]
    public async Task SaveConfig_ParameterAddressEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 2, // API = 2
                    ProtocolType = 200, // Api = 200
                    RequestMethod = 0, // Get = 0
                    AccessApiString = "https://api.example.com",
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            Parameters = new[]
                            {
                                new { Label = "数据", Address = "" }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-PM-MODBUS-01: ModbusTcpNet缺少StationNo")]
    public async Task SaveConfig_ModbusMissingStationNo_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 0,  // ModbusTcpNet = 0
                    IpAddress = "192.168.1.200",
                    ProtocolPort = 502,
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "温度",
                                    Address = "40001",
                                    DataFormat = 0, // ABCD = 0
                                    DataType = 7,   // Float = 7
                                    AddressStartWithZero = true
                                    // 缺少 StationNo
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "TC-PM-CJT188-01: CJT1882004OverTcp缺少InstrumentType")]
    public async Task SaveConfig_CJT188MissingInstrumentType_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 0, // LAN = 0
                    ProtocolType = 11, // CJT1882004OverTcp = 11
                    IpAddress = "192.168.1.200",
                    ProtocolPort = 502,
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            EquipmentType = 1, // Instrument = 1
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "水表读数",
                                    Address = "0001",
                                    StationNo = "1122334455667788",
                                    DataType = 7 // Float = 7
                                    // 缺少 InstrumentType
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region 边界条件测试

    [Fact(DisplayName = "TC-PM-06: MinValue大于MaxValue")]
    public async Task SaveConfig_MinValueGreaterThanMaxValue_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 2, // API = 2
                    ProtocolType = 200, // Api = 200
                    RequestMethod = 0, // Get = 0
                    AccessApiString = "https://api.example.com",
                    Equipments = new[]
                    {
                        new
                        {
                            Id = "E-001",
                            Parameters = new[]
                            {
                                new
                                {
                                    Label = "数据",
                                    Address = "$.value",
                                    MinValue = "100",
                                    MaxValue = "50"
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region 服务器错误测试

    [Fact(DisplayName = "服务器内部错误时返回500")]
    public async Task SaveConfig_InternalServerError_Returns500()
    {
        // Arrange
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<IndustrialDataProcessor.Domain.Entities.WorkstationConfigEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("模拟数据库错误"));

        var request = new
        {
            Protocols = new[]
            {
                new
                {
                    Id = "P-001",
                    InterfaceType = 2, // API = 2
                    ProtocolType = 200, // Api = 200
                    RequestMethod = 0, // Get = 0
                    AccessApiString = "https://api.example.com",
                    Equipments = new[] { new { Id = "E-001", Parameters = new object[] { } } }
                }
            }
        };

        // Act
        var (statusCode, content) = await PostConfigAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion
}
