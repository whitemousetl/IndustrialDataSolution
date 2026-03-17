using FluentAssertions;
using IndustrialDataProcessor.Domain.Entities;
using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
using IndustrialDataProcessor.Infrastructure.Persistence.Repositories;
using IndustrialDataProcessor.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Infrastructure.Tests;

public class WorkstationConfigJsonDeserializationTests
{
    private readonly JsonSerializerOptions _jsonOptions;
    private const string TestJson = """
        {"Id": "WS-001", "Name": "", "IpAddress": "192.168.1.100", "Protocols": [{"Id": "P-001", "Remark": "", "Account": "", "Gateway": "", "Password": "", "IpAddress": "127.0.0.1", "Equipments": [{"Id": "E-001", "Name": "", "IsCollect": true, "Parameters": [{"Cycle": 0, "Label": "Temperature0", "Value": "", "Length": 0, "Address": "D1", "DataType": 1, "MaxValue": "", "MinValue": "", "IsMonitor": false, "StationNo": "1", "DataFormat": 2, "DefaultValue": "", "HasExpression": true, "HasRangeLimit": false, "InstrumentType": null, "IsVirtualPoint": false, "PositiveExpression": "x*10", "AddressStartWithZero": true}, {"Cycle": 0, "Label": "Temperature1", "Value": "", "Length": 0, "Address": "D100", "DataType": 1, "MaxValue": "", "MinValue": "", "IsMonitor": false, "StationNo": "1", "DataFormat": 2, "DefaultValue": "", "HasExpression": false, "HasRangeLimit": false, "InstrumentType": null, "IsVirtualPoint": false, "PositiveExpression": "", "AddressStartWithZero": true}], "EquipmentType": 1}], "ProtocolPort": 9600, "ProtocolType": 2, "InterfaceType": 0, "ConnectTimeOut": 500, "ReceiveTimeOut": 500, "AdditionalOptions": "", "CommunicationDelay": 1000}]}
        """;

    public WorkstationConfigJsonDeserializationTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.Strict,
            MaxDepth = 64,
            Converters = { new ProtocolConfigPolymorphicConverter(), new JsonStringEnumConverter() }
        };
    }

    [Fact]
    public void Deserialize_WorkstationConfig_ShouldPopulateAllProperties()
    {
        // Act
        var config = JsonSerializer.Deserialize<WorkstationConfig>(TestJson, _jsonOptions);

        // Assert - 验证根级别属性
        config.Should().NotBeNull();
        config!.Id.Should().Be("WS-001");
        config.IpAddress.Should().Be("192.168.1.100");
        config.Name.Should().Be("");

        // Assert - 验证Protocols
        config.Protocols.Should().NotBeNull();
        config.Protocols.Should().HaveCount(1);

        var protocol = config.Protocols[0];
        protocol.Id.Should().Be("P-001");
        
        // 强转为NetworkProtocolConfig来访问IpAddress
        var networkProtocol = protocol as NetworkProtocolConfig;
        networkProtocol.Should().NotBeNull();
        networkProtocol!.IpAddress.Should().Be("127.0.0.1");
        
        // Assert - 验证Equipments
        protocol.Equipments.Should().NotBeNull();
        protocol.Equipments.Should().HaveCount(1);

        var equipment = protocol.Equipments[0];
        equipment.Id.Should().Be("E-001");
        equipment.IsCollect.Should().BeTrue();

        // Assert - 验证Parameters
        equipment.Parameters.Should().NotBeNull();
        equipment.Parameters.Should().HaveCount(2);
        equipment.Parameters![0].Label.Should().Be("Temperature0");
        equipment.Parameters[1].Label.Should().Be("Temperature1");
    }

    [Fact]
    public async Task WorkstationConfigRepository_GetLatestParsedConfigAsync_ShouldReturnCorrectData()
    {
        // Arrange - 模拟 IWorkstationConfigPersistenceRepository
        var mockEntityRepo = new Mock<IWorkstationConfigPersistenceRepository>();
        var mockLogger = new Mock<ILogger<WorkstationConfigRepository>>();

        var entity = new WorkstationConfigEntity
        {
            JsonContent = TestJson,
            CreatedAt = DateTimeOffset.Now
        };

        mockEntityRepo
            .Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var repository = new WorkstationConfigRepository(mockEntityRepo.Object, mockLogger.Object, _jsonOptions);

        // Act
        var config = await repository.GetLatestParsedConfigAsync(CancellationToken.None);

        // Assert
        config.Should().NotBeNull();
        config!.Id.Should().Be("WS-001");
        config.IpAddress.Should().Be("192.168.1.100");
        config.Protocols.Should().HaveCount(1);
        config.Protocols[0].Id.Should().Be("P-001");
        config.Protocols[0].Equipments.Should().HaveCount(1);
        config.Protocols[0].Equipments[0].Parameters.Should().HaveCount(2);
    }
}
