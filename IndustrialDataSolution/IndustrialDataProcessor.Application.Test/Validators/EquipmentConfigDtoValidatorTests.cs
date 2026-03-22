using FluentValidation.TestHelper;
using IndustrialDataProcessor.Application.Validators;
using IndustrialDataProcessor.Contracts.WorkstationDto;
using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Application.Tests.Validators;

[Trait("UnitTests", "EquipmentConfigDto")]
public class EquipmentConfigDtoValidatorTests
{
    private readonly EquipmentConfigDtoValidator _validator;

    public EquipmentConfigDtoValidatorTests()
    {
        _validator = new EquipmentConfigDtoValidator();
    }

    #region 基础必填字段验证

    [Theory(DisplayName = "Id为空/空白/null时应该返回验证异常")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Id_WhenEmpty_ShouldHaveValidationError(string? id)
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Id = id!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage($"协议类型: {dto.ProtocolType} 设备[Id]不能为空");
    }

    [Fact(DisplayName = "Id有效时没有验证异常")]
    public void Id_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Id = "Equipment001";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region 枚举值验证

    // 注意：EquipmentConfigDtoValidator 不验证 ProtocolType，它只是从上层传递下来用于错误消息和子验证器
    // [Fact(DisplayName = "ProtocolType超出枚举范围时应该返回校验异常")]
    // public void ProtocolType_WhenInvalidEnum_ShouldHaveValidationError() { ... }

    [Fact(DisplayName = "ProtocolType为有效枚举值时没有验证异常")]
    public void ProtocolType_WhenValidEnum_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.ProtocolType = ProtocolType.ModbusTcpNet;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "EquipmentType超出枚举范围时应该返回校验异常")]
    public void EquipmentType_WhenInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.EquipmentType = (EquipmentType)999; // 无效的枚举值

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EquipmentType)
            .WithErrorMessage($"协议类型: {dto.ProtocolType} 设备: {dto.Id} 的[设备类型]值无效");
    }

    [Fact(DisplayName = "EquipmentType为有效枚举值时没有验证异常")]
    public void EquipmentType_WhenValidEnum_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.EquipmentType = EquipmentType.Equipment;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region 参数列表验证

    // 注意：EquipmentConfigDtoValidator 不验证 Parameters，它可为空（默认为空集合）
    // 以下测试已注释，因为业务规则允许 Parameters 为空
    // [Fact(DisplayName = "Parameters为null时应该返回校验异常")]
    // public void Parameters_WhenNull_ShouldHaveValidationError() { ... }
    // [Fact(DisplayName = "Parameters为空集合时应该返回校验异常")]
    // public void Parameters_WhenEmpty_ShouldHaveValidationError() { ... }

    [Fact(DisplayName = "Parameters为null时没有验证异常（可为空）")]
    public void Parameters_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Parameters = null;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - Parameters 可为空，不验证
        result.ShouldNotHaveValidationErrorFor(x => x.Parameters);
    }

    [Fact(DisplayName = "Parameters为空集合时没有验证异常（可为空）")]
    public void Parameters_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Parameters = new List<ParameterConfigDto>();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - Parameters 可为空，不验证
        result.ShouldNotHaveValidationErrorFor(x => x.Parameters);
    }

    [Fact(DisplayName = "Parameters包含有效参数时没有验证异常")]
    public void Parameters_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region 参数列表子项验证

    [Fact(DisplayName = "Parameters包含无效的Label时应该返回校验异常")]
    public void Parameters_WhenContainsInvalidLabel_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Parameters![0].Label = string.Empty; // 设置一个无效的 Label

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Parameters[0].Label");
    }

    [Fact(DisplayName = "Parameters包含无效的Address时应该返回校验异常")]
    public void Parameters_WhenContainsInvalidAddress_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Parameters![0].Address = string.Empty; // 设置一个无效的 Address

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Parameters[0].Address");
    }

    [Fact(DisplayName = "Parameters包含无效的DataType时应该返回校验异常")]
    public void Parameters_WhenContainsInvalidDataType_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Parameters![0].DataType = (DataType)999; // 无效的枚举值

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Parameters[0].DataType");
    }

    [Fact(DisplayName = "Parameters包含无效的Cycle时应该返回校验异常")]
    public void Parameters_WhenContainsNegativeCycle_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Parameters![0].Cycle = -1; // 负数周期

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Parameters[0].Cycle");
    }

    [Fact(DisplayName = "Parameters包含MinValue大于MaxValue时应该返回校验异常")]
    public void Parameters_WhenMinValueGreaterThanMaxValue_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Parameters![0].MinValue = "100";
        dto.Parameters[0].MaxValue = "50";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Parameters[0].MaxValue");
    }

    [Fact(DisplayName = "Parameters包含多个参数且全部有效时没有验证异常")]
    public void Parameters_WhenMultipleValidParameters_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.Parameters!.Add(new ParameterConfigDto
        {
            Label = "Pressure",
            Address = "400002",
            IsMonitor = true,
            StationNo = "1",
            DataFormat = DomainDataFormat.ABCD,
            DataType = DataType.Float,
            Length = 2,
            Cycle = 2000,
            MinValue = "0",
            MaxValue = "200",
            ProtocolType = ProtocolType.ModbusTcpNet,
            EquipmentId = "Equipment001",
            AddressStartWithZero = true
        });

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Parameters包含协议要求的必填字段缺失时应该返回校验异常")]
    public void Parameters_WhenRequiredProtocolFieldMissing_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();
        dto.ProtocolType = ProtocolType.ModbusTcpNet;
        dto.Parameters![0].StationNo = null; // ModbusTcpNet 要求 StationNo

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Parameters[0].StationNo");
    }

    #endregion

    #region 综合测试

    [Fact(DisplayName = "完全有效的EquipmentConfigDto应该通过所有验证")]
    public void EquipmentConfigDto_WhenFullyValid_ShouldPassAllValidations()
    {
        // Arrange
        var dto = CreateValidEquipmentDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "多个字段同时无效时应该返回所有验证异常")]
    public void EquipmentConfigDto_WhenMultipleFieldsInvalid_ShouldReturnAllErrors()
    {
        // Arrange
        var dto = new EquipmentConfigDto
        {
            Id = string.Empty,                    // 无效：Id 为空
            ProtocolType = (ProtocolType)999,     // ProtocolType 不验证，仅传递用
            EquipmentType = (EquipmentType)999,   // 无效：EquipmentType 超出范围
            Parameters = null                      // Parameters 可为空，不验证
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - 只验证 Id 和 EquipmentType
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.EquipmentType);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 创建一个完全有效的 EquipmentConfigDto 对象
    /// </summary>
    private static EquipmentConfigDto CreateValidEquipmentDto()
    {
        return new EquipmentConfigDto
        {
            Id = "Equipment001",
            IsCollect = true,
            Name = "Test Equipment",
            EquipmentType = EquipmentType.Equipment,
            ProtocolType = ProtocolType.ModbusTcpNet,
            Parameters =
            [
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
                    EquipmentId = "Equipment001",
                    AddressStartWithZero = true
                }
            ]
        };
    }

    #endregion
}
