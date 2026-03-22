using FluentValidation.TestHelper;
using IndustrialDataProcessor.Application.Validators;
using IndustrialDataProcessor.Contracts.WorkstationDto;
using IndustrialDataProcessor.Domain.Enums;
using System.IO.Ports;

namespace IndustrialDataProcessor.Application.Tests.Validators;

[Trait("UnitTests", "ProtocolConfigDto")]
public class ProtocolConfigDtoValidatorTests
{
    private readonly ProtocolConfigDtoValidator _validator;

    public ProtocolConfigDtoValidatorTests()
    {
        _validator = new ProtocolConfigDtoValidator();
    }

    #region 通用必填字段验证

    [Theory(DisplayName = "Id为空/空白/null时应该返回验证异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Id_WhenEmpty_ShouldHaveValidationError(string? id)
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.Id = id!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("协议Id不能为空");
    }

    [Fact(DisplayName = "Id有效时没有验证异常")]
    public void Id_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.Id = "Protocol001";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "InterfaceType超出枚举范围时应该返回校验异常")]
    public void InterfaceType_WhenInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        // 注意：InterfaceType 是抽象属性，需要通过具体实现类测试
        // 这里我们测试其他枚举验证
        dto.ProtocolType = (ProtocolType)999;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProtocolType)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型无效");
    }

    [Fact(DisplayName = "Equipments为null时应该返回校验异常")]
    public void Equipments_WhenNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.Equipments = null!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Equipments)
            .WithErrorMessage($"协议Id: {dto.Id} 设备列表不能为null");
    }

    [Fact(DisplayName = "Equipments为空集合时应该返回校验异常")]
    public void Equipments_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.Equipments = new List<EquipmentConfigDto>();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Equipments)
            .WithErrorMessage($"协议Id: {dto.Id} 设备列表不能为空，至少需要一个设备配置");
    }

    #endregion

    #region 接口类型与协议类型兼容性验证

    [Fact(DisplayName = "接口类型与协议类型不兼容时应该返回校验异常")]
    public void ProtocolType_WhenIncompatibleWithInterfaceType_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        // 假设 OpcUa 不支持 LAN 接口（根据实际业务逻辑调整）
        dto.ProtocolType = ProtocolType.ModbusRtu;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - 如果不兼容会有错误
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage($"协议Id: {dto.Id} 接口类型 {dto.InterfaceType} 不支持协议类型 {dto.ProtocolType}");
    }

    #endregion

    #region 串口相关验证 (InterfaceType.COM)

    [Theory(DisplayName = "COM接口SerialPortName为空/空白/null时应该返回校验异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void SerialPortName_WhenComAndEmpty_ShouldHaveValidationError(string? serialPortName)
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.SerialPortName = serialPortName;
        dto.InterfaceType = InterfaceType.COM;
        dto.ProtocolType = ProtocolType.ModbusRtu;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SerialPortName)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 串口名称不能为空");
    }

    [Fact(DisplayName = "COM接口BaudRate为null时应该返回校验异常")]
    public void BaudRate_WhenComAndNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.InterfaceType = InterfaceType.COM;
        dto.ProtocolType = ProtocolType.ModbusRtu;
        dto.BaudRate = null;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BaudRate)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 波特率不能为空");
    }

    [Fact(DisplayName = "COM接口DataBits为null时应该返回校验异常")]
    public void DataBits_WhenComAndNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.DataBits = null;
        dto.InterfaceType = InterfaceType.COM;
        dto.ProtocolType = ProtocolType.ModbusRtu;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DataBits)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 数据位不能为空");
    }

    [Fact(DisplayName = "COM接口Parity为null时应该返回校验异常")]
    public void Parity_WhenComAndNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.Parity = null;
        dto.InterfaceType = InterfaceType.COM;
        dto.ProtocolType = ProtocolType.ModbusRtu;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Parity)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 校验位不能为空");
    }

    [Fact(DisplayName = "COM接口StopBits为null时应该返回校验异常")]
    public void StopBits_WhenComAndNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.StopBits = null;
        dto.InterfaceType = InterfaceType.COM;
        dto.ProtocolType = ProtocolType.ModbusRtu;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StopBits)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 停止位不能为空");
    }

    [Fact(DisplayName = "COM接口所有必填字段有效时没有验证异常")]
    public void ComProtocol_WhenAllFieldsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.InterfaceType = InterfaceType.COM;
        dto.ProtocolType = ProtocolType.ModbusRtu;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region 网口相关验证 (InterfaceType.LAN)

    [Theory(DisplayName = "LAN接口IpAddress为空/空白/null时应该返回校验异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IpAddress_WhenLanAndEmpty_ShouldHaveValidationError(string? ipAddress)
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.IpAddress = ipAddress;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IpAddress)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} IP地址不能为空");
    }

    [Theory(DisplayName = "LAN接口IpAddress格式不正确时应该返回校验异常")]
    [InlineData("192.168.1.1.1")]
    [InlineData("abc.def.ghi.jkl")]
    [InlineData("999.999.999.999")]
    public void IpAddress_WhenLanAndInvalidFormat_ShouldHaveValidationError(string ipAddress)
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.IpAddress = ipAddress;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IpAddress)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} IP地址格式不正确");
    }

    [Theory(DisplayName = "LAN接口IpAddress格式正确时没有验证异常")]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0")]
    public void IpAddress_WhenLanAndValidFormat_ShouldNotHaveValidationError(string ipAddress)
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.IpAddress = ipAddress;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "LAN接口ProtocolPort为null时应该返回校验异常")]
    public void ProtocolPort_WhenLanAndNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.ProtocolPort = null;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProtocolPort)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 端口不能为空");
    }

    [Fact(DisplayName = "LAN接口ProtocolPort为0或负数时应该返回校验异常")]
    public void ProtocolPort_WhenLanAndZeroOrNegative_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.ProtocolPort = 0;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProtocolPort)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 端口必须大于0");
    }

    [Fact(DisplayName = "LAN接口ProtocolPort超过65535时应该返回校验异常")]
    public void ProtocolPort_WhenLanAndExceeds65535_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.ProtocolPort = 65536;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProtocolPort)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 端口不能超过65535");
    }

    [Theory(DisplayName = "LAN接口ProtocolPort在有效范围内时没有验证异常")]
    [InlineData(1)]
    [InlineData(80)]
    [InlineData(502)]
    [InlineData(65535)]
    public void ProtocolPort_WhenLanAndValid_ShouldNotHaveValidationError(int port)
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.ProtocolPort = port;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProtocolPort);
    }

    [Fact(DisplayName = "LAN接口所有必填字段有效时没有验证异常")]
    public void LanProtocol_WhenAllFieldsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region 数据库相关验证 (InterfaceType.DATABASE)

    [Theory(DisplayName = "DATABASE接口QuerySqlString为空/空白/null时应该返回校验异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void QuerySqlString_WhenDatabaseAndEmpty_ShouldHaveValidationError(string? querySqlString)
    {
        // Arrange
        var dto = CreateValidDatabaseProtocolDto();
        dto.QuerySqlString = querySqlString;
        dto.InterfaceType = InterfaceType.DATABASE;
        dto.ProtocolType = ProtocolType.MySQL;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.QuerySqlString)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 查询SQL语句不能为空");
    }

    [Fact(DisplayName = "DATABASE接口连接字符串和拆分属性都为空时应该返回校验异常")]
    public void DatabaseConnectString_WhenBothEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDatabaseProtocolDto();
        dto.DatabaseConnectString = null;
        dto.IpAddress = null;
        dto.ProtocolPort = null;
        dto.DatabaseName = null;
        dto.InterfaceType = InterfaceType.DATABASE;
        dto.ProtocolType = ProtocolType.MySQL;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DatabaseConnectString)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 数据库连接字符串不能为空");
    }

    [Fact(DisplayName = "DATABASE接口只提供连接字符串时没有验证异常")]
    public void DatabaseConnectString_WhenOnlyConnectStringProvided_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDatabaseProtocolDto();
        dto.DatabaseConnectString = "Server=localhost;Database=test;";
        dto.IpAddress = null;
        dto.ProtocolPort = null;
        dto.DatabaseName = null;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DatabaseConnectString);
    }

    [Fact(DisplayName = "DATABASE接口同时提供两种方式时没有验证异常")]
    public void DatabaseConnectString_WhenBothProvided_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDatabaseProtocolDto();
        dto.DatabaseConnectString = "Server=localhost;Database=test;";
        dto.IpAddress = "192.168.1.100";
        dto.ProtocolPort = 3306;
        dto.DatabaseName = "TestDB";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DatabaseConnectString);
    }

    [Fact(DisplayName = "DATABASE接口所有必填字段有效时没有验证异常")]
    public void DatabaseProtocol_WhenAllFieldsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDatabaseProtocolDto();
        dto.InterfaceType = InterfaceType.DATABASE;
        dto.ProtocolType = ProtocolType.MySQL;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region API相关验证 (InterfaceType.API)

    [Fact(DisplayName = "API接口RequestMethod为null时应该返回校验异常")]
    public void RequestMethod_WhenApiAndNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidApiProtocolDto();
        dto.InterfaceType = InterfaceType.API;
        dto.ProtocolType = ProtocolType.Api;
        dto.RequestMethod = null;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RequestMethod)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 请求方式不能为空");
    }

    [Theory(DisplayName = "API接口AccessApiString为空/空白/null时应该返回校验异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void AccessApiString_WhenApiAndEmpty_ShouldHaveValidationError(string? accessApiString)
    {
        // Arrange
        var dto = CreateValidApiProtocolDto();
        dto.AccessApiString = accessApiString;
        dto.InterfaceType = InterfaceType.API;
        dto.ProtocolType = ProtocolType.Api;
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessApiString)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 访问API地址不能为空");
    }

    [Fact(DisplayName = "API接口所有必填字段有效时没有验证异常")]
    public void ApiProtocol_WhenAllFieldsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidApiProtocolDto();
        dto.InterfaceType = InterfaceType.API;
        dto.ProtocolType = ProtocolType.Api;
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region 设备列表子项验证

    [Fact(DisplayName = "Equipments包含无效的设备Id时应该返回校验异常")]
    public void Equipments_WhenContainsInvalidId_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.Equipments[0].Id = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Equipments[0].Id");
    }

    [Fact(DisplayName = "Equipments包含无效的设备参数时应该返回校验异常")]
    public void Equipments_WhenContainsInvalidParameter_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.Equipments[0].Parameters![0].Label = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Equipments[0].Parameters[0].Label");
    }

    [Fact(DisplayName = "Equipments包含多个有效设备时没有验证异常")]
    public void Equipments_WhenMultipleValidEquipments_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.Equipments.Add(CreateValidEquipment("Equipment002"));

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region 综合测试

    [Fact(DisplayName = "完全有效的LAN协议配置应该通过所有验证")]
    public void ProtocolConfigDto_WhenLanAndFullyValid_ShouldPassAllValidations()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "完全有效的COM协议配置应该通过所有验证")]
    public void ProtocolConfigDto_WhenComAndFullyValid_ShouldPassAllValidations()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.InterfaceType = InterfaceType.COM;
        dto.ProtocolType = ProtocolType.ModbusRtu;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "完全有效的DATABASE协议配置应该通过所有验证")]
    public void ProtocolConfigDto_WhenDatabaseAndFullyValid_ShouldPassAllValidations()
    {
        // Arrange
        var dto = CreateValidDatabaseProtocolDto();
        dto.InterfaceType = InterfaceType.DATABASE;
        dto.ProtocolType = ProtocolType.MySQL;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "完全有效的API协议配置应该通过所有验证")]
    public void ProtocolConfigDto_WhenApiAndFullyValid_ShouldPassAllValidations()
    {
        // Arrange
        var dto = CreateValidApiProtocolDto();
        dto.InterfaceType = InterfaceType.API;
        dto.ProtocolType = ProtocolType.Api;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "多个字段同时无效时应该返回所有验证异常")]
    public void ProtocolConfigDto_WhenMultipleFieldsInvalid_ShouldReturnAllErrors()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.Id = string.Empty;            // 无效：Id 为空
        dto.IpAddress = "invalid";        // 无效：IP 格式错误
        dto.ProtocolPort = 0;             // 无效：端口为 0
        dto.Equipments = null!;           // 无效：设备列表为 null

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.IpAddress);
        result.ShouldHaveValidationErrorFor(x => x.ProtocolPort);
        result.ShouldHaveValidationErrorFor(x => x.Equipments);
    }

    #endregion

    #region COM接口枚举验证

    [Fact(DisplayName = "COM接口BaudRate无效枚举值时应该返回校验异常")]
    public void BaudRate_WhenComAndInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.BaudRate = (BaudRateType)999;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BaudRate)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 波特率无效");
    }

    [Fact(DisplayName = "COM接口DataBits无效枚举值时应该返回校验异常")]
    public void DataBits_WhenComAndInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.DataBits = (DataBitsType)999;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DataBits)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 数据位无效");
    }

    [Fact(DisplayName = "COM接口Parity无效枚举值时应该返回校验异常")]
    public void Parity_WhenComAndInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.Parity = (DomainParity)999;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Parity)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 校验位无效");
    }

    [Fact(DisplayName = "COM接口StopBits无效枚举值时应该返回校验异常")]
    public void StopBits_WhenComAndInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidComProtocolDto();
        dto.StopBits = (DomainStopBits)999;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StopBits)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 停止位无效");
    }

    #endregion

    #region API接口枚举验证

    [Fact(DisplayName = "API接口RequestMethod无效枚举值时应该返回校验异常")]
    public void RequestMethod_WhenApiAndInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidApiProtocolDto();
        dto.RequestMethod = (RequestMethod)999;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RequestMethod)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 请求方式无效");
    }

    [Theory(DisplayName = "API接口RequestMethod为有效枚举值时没有验证异常")]
    [InlineData(RequestMethod.Get)]
    [InlineData(RequestMethod.Post)]
    [InlineData(RequestMethod.Put)]
    [InlineData(RequestMethod.Delete)]
    [InlineData(RequestMethod.Patch)]
    public void RequestMethod_WhenApiAndValidEnum_ShouldNotHaveValidationError(RequestMethod requestMethod)
    {
        // Arrange
        var dto = CreateValidApiProtocolDto();
        dto.RequestMethod = requestMethod;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RequestMethod);
    }

    #endregion

    #region 端口边界值测试

    [Theory(DisplayName = "LAN接口ProtocolPort边界值1和65535时没有验证异常")]
    [InlineData(1)]
    [InlineData(65535)]
    public void ProtocolPort_WhenLanAndBoundaryValues_ShouldNotHaveValidationError(int port)
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.ProtocolPort = port;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProtocolPort);
    }

    [Fact(DisplayName = "LAN接口ProtocolPort为负数时应该返回校验异常")]
    public void ProtocolPort_WhenLanAndNegative_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidLanProtocolDto();
        dto.ProtocolPort = -1;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProtocolPort)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 端口必须大于0");
    }

    #endregion

    #region DATABASE接口补充验证

    [Theory(DisplayName = "DATABASE接口DatabaseName为空/空白/null时应该返回校验异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void DatabaseName_WhenDatabaseAndEmpty_ShouldHaveValidationError(string? databaseName)
    {
        // Arrange
        var dto = CreateValidDatabaseProtocolDto();
        dto.DatabaseName = databaseName;
        dto.InterfaceType = InterfaceType.DATABASE;
        dto.ProtocolType = ProtocolType.MySQL;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DatabaseName)
            .WithErrorMessage($"协议Id: {dto.Id} 协议类型: {dto.ProtocolType} 数据库名称不能为空");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 创建一个有效的 LAN 协议配置
    /// </summary>
    private static TestProtocolConfigDto CreateValidLanProtocolDto()
    {
        return new TestProtocolConfigDto
        {
            Id = "Protocol001",
            InterfaceTypeValue = InterfaceType.LAN,
            ProtocolType = ProtocolType.ModbusTcpNet,
            IpAddress = "192.168.1.100",
            ProtocolPort = 502,
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
    private static TestProtocolConfigDto CreateValidComProtocolDto()
    {
        return new TestProtocolConfigDto
        {
            Id = "Protocol002",
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
    /// 创建一个有效的 DATABASE 协议配置
    /// </summary>
    private static TestProtocolConfigDto CreateValidDatabaseProtocolDto()
    {
        return new TestProtocolConfigDto
        {
            Id = "Protocol003",
            InterfaceTypeValue = InterfaceType.DATABASE,
            ProtocolType = ProtocolType.MySQL,
            DatabaseConnectString = "Server=localhost;Database=test;",
            DatabaseName = "TestDB",
            QuerySqlString = "SELECT * FROM data",
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
    /// 创建一个有效的 API 协议配置
    /// </summary>
    private static TestProtocolConfigDto CreateValidApiProtocolDto()
    {
        return new TestProtocolConfigDto
        {
            Id = "Protocol004",
            InterfaceTypeValue = InterfaceType.API,
            ProtocolType = ProtocolType.Api,
            RequestMethod = RequestMethod.Get,
            AccessApiString = "https://api.example.com/data",
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
                new ParameterConfigDto
                {
                    Label = "Temperature",
                    Address = "400001",
                    IsMonitor = true,
                    StationNo = "1",
                    DataFormat = DomainDataFormat.ABCD,
                    DataType = DataType.Float,
                    Length = 2,
                    Cycle = 1000,
                    MinValue = "0",
                    MaxValue = "100",
                    ProtocolType = ProtocolType.ModbusTcpNet,
                    EquipmentId = equipmentId,
                    AddressStartWithZero = true
                }
            }
        };
    }

    /// <summary>
    /// 测试用的 ProtocolConfigDto 实现类
    /// </summary>
    private class TestProtocolConfigDto : ProtocolConfigDto
    {
        public InterfaceType InterfaceTypeValue
        {
            get => InterfaceType;
            set => InterfaceType = value;
        }
    }

    #endregion
}