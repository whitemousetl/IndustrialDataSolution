//using FluentValidation;
//using FluentValidation.Results;
//using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
//using IndustrialDataProcessor.Application.Services;
//using IndustrialDataProcessor.Domain.Entities;
//using IndustrialDataProcessor.Domain.Enums;
//using IndustrialDataProcessor.Domain.Repositories;
//using Moq;
//using System.Text.Json;

//namespace IndustrialDataProcessor.Application.Tests.Services;

//[Trait("UnitTests", "WorkstationConfigService")]
//public class WorkstationConfigServiceTests
//{
//    private readonly Mock<IWorkstationConfigEntityRepository> _mockRepository;
//    private readonly Mock<IValidator<WorkstationConfigDto>> _mockValidator;
//    private readonly WorkstationConfigService _service;

//    public WorkstationConfigServiceTests()
//    {
//        _mockRepository = new Mock<IWorkstationConfigEntityRepository>();
//        _mockValidator = new Mock<IValidator<WorkstationConfigDto>>();
//        _service = new WorkstationConfigService(
//            _mockRepository.Object,
//            _mockValidator.Object);
//    }

//    #region 构造函数验证

//    [Fact(DisplayName = "构造函数当repository为null时应该抛出ArgumentNullException")]
//    public void Constructor_WhenRepositoryIsNull_ShouldThrowArgumentNullException()
//    {
//        // Arrange & Act & Assert
//        var exception = Assert.Throws<ArgumentNullException>(() =>
//            new WorkstationConfigService(null!, _mockValidator.Object));

//        Assert.Equal("repository", exception.ParamName);
//    }

//    [Fact(DisplayName = "构造函数当validator为null时应该抛出ArgumentNullException")]
//    public void Constructor_WhenValidatorIsNull_ShouldThrowArgumentNullException()
//    {
//        // Arrange & Act & Assert
//        var exception = Assert.Throws<ArgumentNullException>(() =>
//            new WorkstationConfigService(_mockRepository.Object, null!));

//        Assert.Equal("validator", exception.ParamName);
//    }


//    [Fact(DisplayName = "构造函数当所有参数有效时应该成功创建实例")]
//    public void Constructor_WhenAllParametersValid_ShouldCreateInstance()
//    {
//        // Arrange & Act
//        var service = new WorkstationConfigService(
//            _mockRepository.Object,
//            _mockValidator.Object);

//        // Assert
//        Assert.NotNull(service);
//    }

//    #endregion

//    #region SaveWorkstationConfigAsync - 参数验证

//    [Fact(DisplayName = "SaveWorkstationConfigAsync当jsonConfig为null时应该抛出ArgumentException")]
//    public async Task SaveWorkstationConfigAsync_WhenJsonConfigIsNull_ShouldThrowArgumentException()
//    {
//        // Arrange
//        string? jsonConfig = null;

//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
//            await _service.SaveWorkstationConfigAsync(jsonConfig!));

//        Assert.Equal("jsonConfig", exception.ParamName);
//    }

//    [Theory(DisplayName = "SaveWorkstationConfigAsync当jsonConfig为空或空白时应该抛出ArgumentException")]
//    [InlineData("")]
//    [InlineData(" ")]
//    [InlineData("   ")]
//    public async Task SaveWorkstationConfigAsync_WhenJsonConfigIsEmptyOrWhitespace_ShouldThrowArgumentException(string jsonConfig)
//    {
//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
//            await _service.SaveWorkstationConfigAsync(jsonConfig));

//        Assert.Equal("jsonConfig", exception.ParamName);
//    }

//    #endregion

//    #region SaveWorkstationConfigAsync - JSON反序列化

//    [Theory(DisplayName = "SaveWorkstationConfigAsync当JSON格式无效时应该抛出ArgumentException")]
//    [InlineData("{invalid json}")]
//    [InlineData("not a json")]
//    [InlineData("[1,2,3]")]
//    public async Task SaveWorkstationConfigAsync_WhenJsonFormatInvalid_ShouldThrowArgumentException(string jsonConfig)
//    {
//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
//            await _service.SaveWorkstationConfigAsync(jsonConfig));

//        Assert.Contains("配置格式错误", exception.Message);
//    }

//    [Fact(DisplayName = "SaveWorkstationConfigAsync当JSON为空对象时应该抛出ValidationException")]
//    public async Task SaveWorkstationConfigAsync_WhenJsonIsEmptyObject_ShouldThrowArgumentException()
//    {
//        // Arrange
//        var jsonConfig = "{}";

//        // 空对象反序列化成功，但验证会失败（缺少必填字段）
//        var validationFailures = new List<ValidationFailure>
//        {
//            new ("Id", "工作站ID不能为空"),
//            new ("IpAddress", "工作站IP地址不能为空"),
//            new ("Protocols", "协议列表不能为空")
//        };
//        var validationResult = new ValidationResult(validationFailures);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(validationResult);

//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
//            await _service.SaveWorkstationConfigAsync(jsonConfig));

//        // 验证异常消息包含验证失败信息
//        Assert.Contains("工作站ID不能为空", exception.Message);
//        Assert.Contains("工作站IP地址不能为空", exception.Message);
//        Assert.Contains("协议列表不能为空", exception.Message);
//    }

//    #endregion

//    #region SaveWorkstationConfigAsync - 验证失败

//    [Fact(DisplayName = "SaveWorkstationConfigAsync当验证失败时应该抛出ValidationException")]
//    public async Task SaveWorkstationConfigAsync_WhenValidationFails_ShouldThrowValidationException()
//    {
//        // Arrange
//        var dto = CreateValidWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        var validationFailures = new List<ValidationFailure>
//        {
//            new ValidationFailure("Id", "工作站ID不能为空"),
//            new ValidationFailure("IpAddress", "IP地址格式不正确")
//        };
//        var validationResult = new ValidationResult(validationFailures);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(validationResult);

//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
//            await _service.SaveWorkstationConfigAsync(jsonConfig));

//        Assert.Contains("工作站ID不能为空", exception.Message);
//        Assert.Contains("IP地址格式不正确", exception.Message);
//    }

//    [Fact(DisplayName = "SaveWorkstationConfigAsync当验证失败时不应该调用repository")]
//    public async Task SaveWorkstationConfigAsync_WhenValidationFails_ShouldNotCallRepository()
//    {
//        // Arrange
//        var dto = CreateValidWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        var validationFailures = new List<ValidationFailure>
//        {
//            new ValidationFailure("Id", "工作站ID不能为空")
//        };
//        var validationResult = new ValidationResult(validationFailures);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(validationResult);

//        // Act
//        try
//        {
//            await _service.SaveWorkstationConfigAsync(jsonConfig);
//        }
//        catch (ValidationException)
//        {
//            // Expected exception
//        }

//        // Assert
//        _mockRepository.Verify(
//            r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), It.IsAny<CancellationToken>()),
//            Times.Never);
//    }

//    #endregion

//    #region SaveWorkstationConfigAsync - 验证成功并保存

//    [Fact(DisplayName = "SaveWorkstationConfigAsync当验证成功时应该保存到数据库")]
//    public async Task SaveWorkstationConfigAsync_WhenValidationSucceeds_ShouldSaveToDatabase()
//    {
//        // Arrange
//        var dto = CreateValidWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        WorkstationConfigEntity? capturedEntity = null;
//        _mockRepository
//            .Setup(r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), It.IsAny<CancellationToken>()))
//            .Callback<WorkstationConfigEntity, CancellationToken>((entity, _) => capturedEntity = entity)
//            .Returns(Task.CompletedTask);

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig);

//        // Assert
//        _mockRepository.Verify(
//            r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), It.IsAny<CancellationToken>()),
//            Times.Once);

//        Assert.NotNull(capturedEntity);
//        Assert.NotNull(capturedEntity.JsonContent);
//        Assert.NotEmpty(capturedEntity.JsonContent);
//    }

//    [Fact(DisplayName = "SaveWorkstationConfigAsync应该调用validator一次")]
//    public async Task SaveWorkstationConfigAsync_ShouldCallValidatorOnce()
//    {
//        // Arrange
//        var dto = CreateValidWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig);

//        // Assert
//        _mockValidator.Verify(
//            v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()),
//            Times.Once);
//    }

//    [Fact(DisplayName = "SaveWorkstationConfigAsync保存的JSON应该是有效的")]
//    public async Task SaveWorkstationConfigAsync_SavedJsonShouldBeValid()
//    {
//        // Arrange
//        var dto = CreateValidWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        string? savedJson = null;
//        _mockRepository
//            .Setup(r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), It.IsAny<CancellationToken>()))
//            .Callback<WorkstationConfigEntity, CancellationToken>((entity, _) => savedJson = entity.JsonContent)
//            .Returns(Task.CompletedTask);

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig);

//        // Assert
//        Assert.NotNull(savedJson);

//        // 验证保存的 JSON 可以反序列化
//        var deserializedDto = JsonSerializer.Deserialize<WorkstationConfigDto>(savedJson);
//        Assert.NotNull(deserializedDto);
//    }

//    #endregion

//    #region SaveWorkstationConfigAsync - 取消令牌

//    [Fact(DisplayName = "SaveWorkstationConfigAsync应该将取消令牌传递给validator")]
//    public async Task SaveWorkstationConfigAsync_ShouldPassCancellationTokenToValidator()
//    {
//        // Arrange
//        var dto = CreateValidWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);
//        var cancellationToken = new CancellationToken();

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig, cancellationToken);

//        // Assert
//        _mockValidator.Verify(
//            v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), cancellationToken),
//            Times.Once);
//    }

//    [Fact(DisplayName = "SaveWorkstationConfigAsync应该将取消令牌传递给repository")]
//    public async Task SaveWorkstationConfigAsync_ShouldPassCancellationTokenToRepository()
//    {
//        // Arrange
//        var dto = CreateValidWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);
//        var cancellationToken = new CancellationToken();

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig, cancellationToken);

//        // Assert
//        _mockRepository.Verify(
//            r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), cancellationToken),
//            Times.Once);
//    }

//    [Fact(DisplayName = "SaveWorkstationConfigAsync当操作被取消时应该传播OperationCanceledException")]
//    public async Task SaveWorkstationConfigAsync_WhenCancelled_ShouldPropagateOperationCanceledException()
//    {
//        // Arrange
//        var dto = CreateValidWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);
//        var cancellationTokenSource = new CancellationTokenSource();
//        cancellationTokenSource.Cancel();

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ThrowsAsync(new OperationCanceledException());

//        // Act & Assert
//        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
//            await _service.SaveWorkstationConfigAsync(jsonConfig, cancellationTokenSource.Token));
//    }

//    #endregion

//    #region SaveWorkstationConfigAsync - 复杂场景

//    [Fact(DisplayName = "SaveWorkstationConfigAsync应该处理包含多个协议的配置")]
//    public async Task SaveWorkstationConfigAsync_ShouldHandleMultipleProtocols()
//    {
//        // Arrange
//        var dto = CreateComplexWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig);

//        // Assert
//        _mockRepository.Verify(
//            r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), It.IsAny<CancellationToken>()),
//            Times.Once);
//    }

//    [Fact(DisplayName = "SaveWorkstationConfigAsync应该处理包含多个设备的配置")]
//    public async Task SaveWorkstationConfigAsync_ShouldHandleMultipleEquipments()
//    {
//        // Arrange
//        var dto = CreateWorkstationDtoWithMultipleEquipments();
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig);

//        // Assert
//        _mockRepository.Verify(
//            r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), It.IsAny<CancellationToken>()),
//            Times.Once);
//    }

//    [Fact(DisplayName = "SaveWorkstationConfigAsync应该处理包含多个参数的配置")]
//    public async Task SaveWorkstationConfigAsync_ShouldHandleMultipleParameters()
//    {
//        // Arrange
//        var dto = CreateWorkstationDtoWithMultipleParameters();
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig);

//        // Assert
//        _mockRepository.Verify(
//            r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), It.IsAny<CancellationToken>()),
//            Times.Once);
//    }

//    #endregion

//    #region SaveWorkstationConfigAsync - 边界情况

//    [Fact(DisplayName = "SaveWorkstationConfigAsync应该处理最小有效配置")]
//    public async Task SaveWorkstationConfigAsync_ShouldHandleMinimalValidConfig()
//    {
//        // Arrange
//        var dto = CreateMinimalValidWorkstationDto();
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig);

//        // Assert
//        _mockRepository.Verify(
//            r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), It.IsAny<CancellationToken>()),
//            Times.Once);
//    }

//    [Fact(DisplayName = "SaveWorkstationConfigAsync应该处理包含特殊字符的配置")]
//    public async Task SaveWorkstationConfigAsync_ShouldHandleSpecialCharacters()
//    {
//        // Arrange
//        var dto = CreateValidWorkstationDto();
//        dto.Id = "工作站-001_测试";
//        var jsonConfig = JsonSerializer.Serialize(dto);

//        _mockValidator
//            .Setup(v => v.ValidateAsync(It.IsAny<WorkstationConfigDto>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new ValidationResult());

//        // Act
//        await _service.SaveWorkstationConfigAsync(jsonConfig);

//        // Assert
//        _mockRepository.Verify(
//            r => r.AddAsync(It.IsAny<WorkstationConfigEntity>(), It.IsAny<CancellationToken>()),
//            Times.Once);
//    }

//    #endregion

//    #region Helper Methods

//    /// <summary>
//    /// 创建一个有效的 WorkstationConfigDto
//    /// </summary>
//    private static TestWorkstationConfigDto CreateValidWorkstationDto()
//    {
//        return new TestWorkstationConfigDto
//        {
//            Id = "Workstation001",
//            IpAddress = "192.168.1.100",
//            Protocols = new List<ProtocolConfigDto>
//            {
//                CreateValidLanProtocol("Protocol001")
//            }
//        };
//    }

//    /// <summary>
//    /// 创建一个包含多个协议的复杂配置
//    /// </summary>
//    private static TestWorkstationConfigDto CreateComplexWorkstationDto()
//    {
//        return new TestWorkstationConfigDto
//        {
//            Id = "Workstation001",
//            IpAddress = "192.168.1.100",
//            Protocols = new List<ProtocolConfigDto>
//            {
//                CreateValidLanProtocol("Protocol001"),
//                CreateValidLanProtocol("Protocol002")
//            }
//        };
//    }

//    /// <summary>
//    /// 创建一个包含多个设备的配置
//    /// </summary>
//    private static TestWorkstationConfigDto CreateWorkstationDtoWithMultipleEquipments()
//    {
//        var protocol = CreateValidLanProtocol("Protocol001");
//        protocol.Equipments.Add(CreateValidEquipment("Equipment002"));

//        return new TestWorkstationConfigDto
//        {
//            Id = "Workstation001",
//            IpAddress = "192.168.1.100",
//            Protocols = new List<ProtocolConfigDto> { protocol }
//        };
//    }

//    /// <summary>
//    /// 创建一个包含多个参数的配置
//    /// </summary>
//    private static TestWorkstationConfigDto CreateWorkstationDtoWithMultipleParameters()
//    {
//        var equipment = CreateValidEquipment("Equipment001");
//        equipment.Parameters!.Add(CreateValidParameter("Pressure", "400002"));
//        equipment.Parameters.Add(CreateValidParameter("Flow", "400003"));

//        var protocol = CreateValidLanProtocol("Protocol001");
//        protocol.Equipments[0] = equipment;

//        return new TestWorkstationConfigDto
//        {
//            Id = "Workstation001",
//            IpAddress = "192.168.1.100",
//            Protocols = new List<ProtocolConfigDto> { protocol }
//        };
//    }

//    /// <summary>
//    /// 创建一个最小有效配置
//    /// </summary>
//    private static TestWorkstationConfigDto CreateMinimalValidWorkstationDto()
//    {
//        return new TestWorkstationConfigDto
//        {
//            Id = "W1",
//            IpAddress = "127.0.0.1",
//            Protocols = new List<ProtocolConfigDto>
//            {
//                CreateMinimalValidProtocol()
//            }
//        };
//    }

//    /// <summary>
//    /// 创建一个有效的 LAN 协议配置
//    /// </summary>
//    private static TestProtocolConfigDto CreateValidLanProtocol(string id)
//    {
//        return new TestProtocolConfigDto
//        {
//            Id = id,
//            InterfaceTypeValue = InterfaceType.LAN,
//            ProtocolType = ProtocolType.ModbusTcpNet,
//            IpAddress = "192.168.1.100",
//            ProtocolPort = 502,
//            Equipments = new List<EquipmentConfigDto>
//            {
//                CreateValidEquipment("Equipment001")
//            }
//        };
//    }

//    /// <summary>
//    /// 创建一个最小有效协议配置
//    /// </summary>
//    private static TestProtocolConfigDto CreateMinimalValidProtocol()
//    {
//        return new TestProtocolConfigDto
//        {
//            Id = "P1",
//            InterfaceTypeValue = InterfaceType.LAN,
//            ProtocolType = ProtocolType.ModbusTcpNet,
//            IpAddress = "127.0.0.1",
//            ProtocolPort = 502,
//            Equipments = new List<EquipmentConfigDto>
//            {
//                CreateValidEquipment("E1")
//            }
//        };
//    }

//    /// <summary>
//    /// 创建一个有效的设备配置
//    /// </summary>
//    private static EquipmentConfigDto CreateValidEquipment(string equipmentId)
//    {
//        return new EquipmentConfigDto
//        {
//            Id = equipmentId,
//            IsCollect = true,
//            Name = "Test Equipment",
//            EquipmentType = EquipmentType.Equipment,
//            ProtocolType = ProtocolType.ModbusTcpNet,
//            Parameters = new List<ParameterConfigDto>
//            {
//                CreateValidParameter("Temperature", "400001")
//            }
//        };
//    }

//    /// <summary>
//    /// 创建一个有效的参数配置
//    /// </summary>
//    private static ParameterConfigDto CreateValidParameter(string label, string address)
//    {
//        return new ParameterConfigDto
//        {
//            Label = label,
//            Address = address,
//            IsMonitor = true,
//            StationNo = "1",
//            DataFormat = DomainDataFormat.ABCD,
//            DataType = DataType.Float,
//            Length = 2,
//            Cycle = 1000,
//            MinValue = "0",
//            MaxValue = "100",
//            ProtocolType = ProtocolType.ModbusTcpNet,
//            EquipmentId = "Equipment001",
//            AddressStartWithZero = true
//        };
//    }

//    /// <summary>
//    /// 测试用的 WorkstationConfigDto 实现类
//    /// </summary>
//    private class TestWorkstationConfigDto : WorkstationConfigDto
//    {
//    }

//    /// <summary>
//    /// 测试用的 ProtocolConfigDto 实现类
//    /// </summary>
//    private class TestProtocolConfigDto : ProtocolConfigDto
//    {
//        public InterfaceType InterfaceTypeValue { get; set; }
//    }

//    #endregion
//}