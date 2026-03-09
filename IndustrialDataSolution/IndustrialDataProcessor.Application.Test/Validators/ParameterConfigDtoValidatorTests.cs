using FluentValidation.TestHelper;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using IndustrialDataProcessor.Application.Validators;
using IndustrialDataProcessor.Domain.Enums;
using System.Data.Common;
using System.Reflection.Metadata;

namespace IndustrialDataProcessor.Application.Tests.Validators;

[Trait("UnitTests", "ParameterConfigDto")]
public class ParameterConfigDtoValidatorTests
{
    private readonly ParameterConfigDtoValidator _validator;

    public ParameterConfigDtoValidatorTests()
    {
        _validator = new ParameterConfigDtoValidator();
    }

    #region 基础必填字段验证

    [Theory(DisplayName = "Label为空/空白/null时应该返回验证异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Label_WhenEmpty_ShouldHaveValidationError(string? label)
    {
        var dto = CreateValidParameterDto();

        dto.Label = label!;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Label).WithErrorMessage($"协议类型: {dto.ProtocolType} 设备: {dto.EquipmentId} 参数[标签]不能为空");
    }

    [Theory(DisplayName = "Address为空/空白/null时应该返回校验异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Address_WhenEmpty_ShouldHaveValidationError(string? address)
    {
        var dto = CreateValidParameterDto();

        dto.Address = address!;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Address).WithErrorMessage($"协议类型: {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 参数[地址]不能为空");
    }

    #endregion

    #region 枚举值验证

    [Fact(DisplayName = "DataType超出枚举范围时应该返回校验异常")]
    public void DataType_WhenInvalidEnum_ShouldHaveVliadationError()
    {
        var dto = CreateValidParameterDto();
        dto.DataType = (DataType)999;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.DataType).WithErrorMessage($"协议类型: {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 的[数据类型]值无效");
    }

    [Fact(DisplayName = "DataFormat超出枚举范围时应该返回校验异常")]
    public void DataFormat_WhenInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidParameterDto();
        dto.DataFormat = (DomainDataFormat)999; // 无效的枚举值

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DataFormat)
            .WithErrorMessage($"协议类型: {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 的[数据格式/字节序]值无效");
    }

    [Fact(DisplayName = "InstrumentType超出枚举范围时应该返回校验异常")]
    public void InstrumentType_WhenInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidParameterDto();
        dto.InstrumentType = (InstrumentType)999; // 无效的枚举值

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InstrumentType)
            .WithErrorMessage($"协议类型: {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 的[仪表类型]值无效");
    }

    #endregion

    #region 数值范围验证
    [Fact(DisplayName = "Cycle少于0时应该返回校验异常")]
    public void Cycle_WhenNegative_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidParameterDto();
        dto.Cycle = -1;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Cycle)
            .WithErrorMessage($"协议类型: {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 的[采集周期]不能为负数");
    }

    [Fact(DisplayName = "Cycle大于等于0时没有任何校验异常")]
    public void Cycle_WhenZeroOrPositive_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidParameterDto();
        dto.Cycle = 0;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region MinValue <= MaxValue 验证

    [Fact(DisplayName = "最小值大于最大值时返回校验异常")]
    public void MinValue_WhenGreaterThanMaxValue_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidParameterDto();
        dto.MinValue = "100";
        dto.MaxValue = "50";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        var msg = $"协议类型: {dto.ProtocolType} 设备: {dto.EquipmentId} 的[最小值](100)不能大于[最大值](50)";
        result.ShouldHaveValidationErrorFor(x => x.MaxValue)
            .WithErrorMessage($"协议类型: {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 的[最小值](100)不能大于[最大值](50)");
    }

    [Fact(DisplayName = "最小值小于最大值时没有任何校验异常")]
    public void MinValue_WhenLessThanOrEqualToMaxValue_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidParameterDto();
        dto.MinValue = "50";
        dto.MaxValue = "100";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "最小值等于最大值时没有任何校验异常")]
    public void MinValue_WhenEqualToMaxValue_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidParameterDto();
        dto.MinValue = "100";
        dto.MaxValue = "100";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "最小值最大值不是数字时没有任何异常")]
    public void MinMaxValue_WhenNotParseable_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidParameterDto();
        dto.MinValue = "not_a_number";
        dto.MaxValue = "also_not_a_number";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion


    #region 协议特定验证

    [Fact(DisplayName = "当协议要求DataFormat但为null时返回校验异常")]
    public void DataFormat_WhenRequiredByProtocolButMissing_ShouldHaveValidationError()
    {
        var dto = CreateValidParameterDto();
        dto.ProtocolType = ProtocolType.ModbusTcpNet;
        dto.DataFormat = null;
        dto.StationNo = "1";
        dto.DataType = DataType.Bool;
        dto.AddressStartWithZero = true;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.DataFormat)
           .WithErrorMessage($" {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 要求参数必须包含[数据格式/字节序]");
    }

    [Theory(DisplayName = "当协议要求StationNo但为空/空白/null时返回校验异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void StationNo_WhenRequiredByProtocolButMissing_ShouldHaveValidationError(string? stationNo)
    {
        var dto = CreateValidParameterDto();
        dto.ProtocolType = ProtocolType.ModbusTcpNet;
        dto.DataFormat = DomainDataFormat.BADC;
        dto.StationNo = stationNo!;
        dto.DataType = DataType.Bool;
        dto.AddressStartWithZero = true;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.StationNo)
           .WithErrorMessage($"协议类型: {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 要求参数必须包含[站号/通讯地址]");
    }

    [Fact(DisplayName = "当协议要求DataType但为null时返回校验异常")]
    public void DataType_WhenRequiredByProtocolButMissing_ShouldHaveValidationError()
    {
        var dto = CreateValidParameterDto();
        dto.ProtocolType = ProtocolType.ModbusTcpNet;
        dto.DataFormat = DomainDataFormat.ABCD;
        dto.StationNo = "1";
        dto.DataType = null;
        dto.AddressStartWithZero = true;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.DataType)
           .WithErrorMessage($"协议类型 {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 要求参数必须包含[数据类型]");
    }

    [Fact(DisplayName = "当协议要求AddressStartWithZero但为null时返回校验异常")]
    public void AddressStartWithZero_WhenRequiredByProtocolButMissing_ShouldHaveValidationError()
    {
        var dto = CreateValidParameterDto();
        dto.ProtocolType = ProtocolType.ModbusTcpNet;
        dto.DataFormat = DomainDataFormat.ABCD;
        dto.StationNo = "1";
        dto.DataType = DataType.Bool;
        dto.AddressStartWithZero = null;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AddressStartWithZero)
           .WithErrorMessage($" {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 要求参数必须包含[地址必须从0开始?]");
    }

    [Fact(DisplayName = "当协议要求InstrumentType但为null时返回校验异常")]
    public void InstrumentType_WhenRequiredByProtocolButMissing_ShouldHaveValidationError()
    {
        var dto = CreateValidParameterDto();
        dto.ProtocolType = ProtocolType.CJT1882004OverTcp;
        dto.DataFormat = null;
        dto.StationNo = "1";
        dto.DataType = DataType.Bool;
        dto.AddressStartWithZero = null;
        dto.InstrumentType = null;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.InstrumentType)
           .WithErrorMessage($" {dto.ProtocolType} 设备: {dto.EquipmentId} 标签: {dto.Label} 要求参数必须包含[仪表类型]");
    }

    [Fact(DisplayName = "当协议是OPCUA时只需要Label,Address,IsMonitor")]
    public void OpcUA_OnlyNeedLabelAddressIsMonitor_ShouldNotHaveValidationError()
    {
        var dto = CreateValidParameterDto();
        dto.Label = "xxjjj";
        dto.Address = "ns=2;s=dsoufy";
        dto.IsMonitor = false;
        dto.ProtocolType = ProtocolType.OpcUa;
        dto.DataFormat = null;
        dto.StationNo = null;
        dto.DataType = null;
        dto.AddressStartWithZero = null;
        dto.InstrumentType = null;
        dto.Length = null;
        dto.DefaultValue = null;
        dto.Cycle = null;
        dto.PositiveExpression = null;
        dto.MinValue = null;
        dto.MaxValue = null;
        dto.Value = null;

        var result = _validator.TestValidate(dto);
       
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Helper Methods
    private static ParameterConfigDto CreateValidParameterDto()
    {
        return new ParameterConfigDto
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
            EquipmentId = "Equipment001",
            AddressStartWithZero = true
        };
    }
    #endregion
}
