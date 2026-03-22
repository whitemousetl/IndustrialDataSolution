using FluentValidation.TestHelper;
using IndustrialDataProcessor.Application.Validators;
using IndustrialDataProcessor.Contracts.WorkstationDto;
using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Application.Tests.Validators;

[Trait("UnitTests", "WorkstationConfigDto")]
public class WorkstationConfigDtoValidatorTests
{
    private readonly WorkstationConfigDtoValidator _validator;

    public WorkstationConfigDtoValidatorTests()
    {
        _validator = new WorkstationConfigDtoValidator();
    }

    #region Id, IpAddress, Name 可为空验证

    [Theory(DisplayName = "Id为空/空白/null时不应该返回验证异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Id_WhenEmpty_ShouldNotHaveValidationError(string? id)
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Id = id!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - Id 可为空，不应该有验证错误
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    [Theory(DisplayName = "IpAddress为空/空白/null时不应该返回验证异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IpAddress_WhenEmpty_ShouldNotHaveValidationError(string? ipAddress)
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.IpAddress = ipAddress!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - IpAddress 可为空，不应该有验证错误
        result.ShouldNotHaveValidationErrorFor(x => x.IpAddress);
    }

    [Fact(DisplayName = "Name为空时不应该返回验证异常")]
    public void Name_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Name = null;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - Name 可为空，不应该有验证错误
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region 协议列表验证 - NotNull 和 NotEmpty

    [Fact(DisplayName = "Protocols为null时应该返回校验异常")]
    public void Protocols_WhenNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols = null!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Protocols)
            .WithErrorMessage("协议列表不能为null");
    }

    [Fact(DisplayName = "Protocols为空集合时应该返回校验异常")]
    public void Protocols_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols = new List<ProtocolConfigDto>();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Protocols)
            .WithErrorMessage("协议列表不能为空，至少需要一个协议配置");
    }

    [Fact(DisplayName = "Protocols包含有效协议时没有验证异常")]
    public void Protocols_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region 协议列表子项验证

    [Fact(DisplayName = "Protocols包含无效的协议Id时应该返回校验异常")]
    public void Protocols_WhenContainsNullId_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols[0].Id = null!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - 协议Id不能为null
        result.ShouldHaveValidationErrorFor("Protocols[0].Id");
    }

    [Fact(DisplayName = "Protocols包含无效的IP地址时应该返回校验异常")]
    public void Protocols_WhenContainsInvalidIpAddress_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols[0].IpAddress = "invalid_ip";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Protocols[0].IpAddress");
    }

    [Fact(DisplayName = "Protocols包含无效的端口时应该返回校验异常")]
    public void Protocols_WhenContainsInvalidPort_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols[0].ProtocolPort = 0;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Protocols[0].ProtocolPort");
    }

    [Fact(DisplayName = "Protocols包含无效的ProtocolType时应该返回校验异常")]
    public void Protocols_WhenContainsInvalidProtocolType_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols[0].ProtocolType = (ProtocolType)999;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Protocols[0].ProtocolType");
    }

    [Fact(DisplayName = "Protocols包含空设备列表时应该返回校验异常")]
    public void Protocols_WhenContainsEmptyEquipments_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols[0].Equipments = new List<EquipmentConfigDto>();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Protocols[0].Equipments");
    }

    [Fact(DisplayName = "Protocols包含多个有效协议时没有验证异常")]
    public void Protocols_WhenMultipleValidProtocols_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols.Add(CreateValidLanProtocol("Protocol002", "192.168.1.101", 503));

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Protocols嵌套验证-设备参数无效时应该返回校验异常")]
    public void Protocols_WhenNestedParameterInvalid_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols[0].Equipments[0].Parameters![0].Label = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Protocols[0].Equipments[0].Parameters[0].Label");
    }

    #endregion

    #region 综合测试

    [Fact(DisplayName = "完全有效的WorkstationConfigDto应该通过所有验证")]
    public void WorkstationConfigDto_WhenFullyValid_ShouldPassAllValidations()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "协议列表为null时应该返回验证异常")]
    public void WorkstationConfigDto_WhenProtocolsNull_ShouldReturnError()
    {
        // Arrange
        var dto = new TestWorkstationConfigDto
        {
            Id = string.Empty,              // 有效：Id 可为空
            IpAddress = "invalid_ip",       // 有效：IpAddress 可为空
            Protocols = null!               // 无效：协议列表不能为 null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
        result.ShouldNotHaveValidationErrorFor(x => x.IpAddress);
        result.ShouldHaveValidationErrorFor(x => x.Protocols);
    }

    [Fact(DisplayName = "包含多种协议类型的工作站配置应该通过验证")]
    public void WorkstationConfigDto_WhenMultipleProtocolTypes_ShouldPassValidation()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Protocols.Add(CreateValidComProtocol("Protocol002"));
        dto.Protocols[1].InterfaceType = InterfaceType.COM;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "包含复杂嵌套结构的有效配置应该通过验证")]
    public void WorkstationConfigDto_WhenComplexValidStructure_ShouldPassValidation()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();

        // 添加第二个协议
        var protocol2 = CreateValidLanProtocol("Protocol002", "192.168.1.101", 503);

        // 为第二个协议添加多个设备
        protocol2.Equipments.Add(CreateValidEquipment("Equipment002"));

        // 为第一个设备添加多个参数
        protocol2.Equipments[0].Parameters!.Add(CreateValidParameter("Pressure", "400002"));

        dto.Protocols.Add(protocol2);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Id和IpAddress为空但Protocols有效时应该通过验证")]
    public void WorkstationConfigDto_WhenIdAndIpAddressEmptyButProtocolsValid_ShouldPass()
    {
        // Arrange
        var dto = CreateValidWorkstationDto();
        dto.Id = string.Empty;
        dto.IpAddress = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 创建一个完全有效的 WorkstationConfigDto 对象
    /// </summary>
    private static TestWorkstationConfigDto CreateValidWorkstationDto()
    {
        return new TestWorkstationConfigDto
        {
            Id = "Workstation001",
            IpAddress = "192.168.1.100",
            Protocols = new List<ProtocolConfigDto>
            {
                CreateValidLanProtocol("Protocol001", "192.168.1.100", 502)
            }
        };
    }

    /// <summary>
    /// 创建一个有效的 LAN 协议配置
    /// </summary>
    private static TestProtocolConfigDto CreateValidLanProtocol(string id, string ipAddress, int port)
    {
        return new TestProtocolConfigDto
        {
            Id = id,
            InterfaceTypeValue = InterfaceType.LAN,
            ProtocolType = ProtocolType.ModbusTcpNet,
            IpAddress = ipAddress,
            ProtocolPort = port,
            CommunicationDelay = 1000,
            ReceiveTimeOut = 500,
            ConnectTimeOut = 500,
            Equipments = new List<EquipmentConfigDto>
            {
                CreateValidEquipment("Equipment001")
            }
        };
    }

    /// <summary>
    /// 创建一个有效的 COM 协议配置
    /// </summary>
    private static TestProtocolConfigDto CreateValidComProtocol(string id)
    {
        return new TestProtocolConfigDto
        {
            Id = id,
            InterfaceTypeValue = InterfaceType.COM,
            ProtocolType = ProtocolType.ModbusRtu,
            SerialPortName = "COM1",
            BaudRate = BaudRateType.B9600,
            DataBits = DataBitsType.D8,
            Parity = DomainParity.None,
            StopBits = DomainStopBits.One,
            CommunicationDelay = 1000,
            ReceiveTimeOut = 500,
            ConnectTimeOut = 500,
            Equipments = new List<EquipmentConfigDto>
            {
                CreateValidEquipment("Equipment001")
            }
        };
    }

    /// <summary>
    /// 创建一个有效的设备配置
    /// </summary>
    private static EquipmentConfigDto CreateValidEquipment(string equipmentId)
    {
        return new EquipmentConfigDto
        {
            Id = equipmentId,
            IsCollect = true,
            Name = "Test Equipment",
            EquipmentType = EquipmentType.Equipment,
            ProtocolType = ProtocolType.ModbusTcpNet,
            Parameters = new List<ParameterConfigDto>
            {
                CreateValidParameter("Temperature", "400001")
            }
        };
    }

    /// <summary>
    /// 创建一个有效的参数配置
    /// </summary>
    private static ParameterConfigDto CreateValidParameter(string label, string address)
    {
        return new ParameterConfigDto
        {
            Label = label,
            Address = address,
            IsMonitor = true,
            StationNo = "1",
            DataFormat = DomainDataFormat.ABCD,
            DataType = DataType.Float,
            Length = 2,
            Cycle = 1000,
            MinValue = "0",
            MaxValue = "100",
            ProtocolType = ProtocolType.ModbusTcpNet,
            EquipmentId = "Equipment001",
            AddressStartWithZero = true
        };
    }

    /// <summary>
    /// 测试用的 WorkstationConfigDto 实现类
    /// </summary>
    private class TestWorkstationConfigDto : WorkstationConfigDto
    {
    }

    /// <summary>
    /// 测试用的 ProtocolConfigDto 实现类（因为原类是抽象类）
    /// </summary>
    private class TestProtocolConfigDto : ProtocolConfigDto
    {
        public InterfaceType InterfaceTypeValue { get; set; }
    }

    #endregion
}