//using FluentAssertions;
//using IndustrialDataProcessor.Application.Dtos;
//using IndustrialDataProcessor.Domain.Repositories;
//using Microsoft.AspNetCore.Mvc.Testing;
//using Microsoft.Extensions.DependencyInjection;
//using Moq;
//using System.Net;
//using System.Net.Http.Json;
//using System.Text.Json;
//using System.Text.Json.Nodes;

//namespace IndustrialDataProcessor.Api.Tests.Integration;

//[Trait("Integration", "WorkstationConfigApiTests")]
//public class WorkstationConfigApiTests : IClassFixture<WebApplicationFactory<Program>>
//{
//    #region 一些准备
//    private readonly HttpClient _client;
//    private readonly WebApplicationFactory<Program> _factory;

//    public WorkstationConfigApiTests(WebApplicationFactory<Program> factory)
//    {
//        _factory = factory;
//        _client = factory.CreateClient();
//    }

//    // =====================================
//    // 辅助方法：发送请求并解析响应
//    // =====================================
//    private async Task<(HttpStatusCode StatusCode, JsonElement Content)> PostConfigAsync(string jsonContent)
//    {
//        var request = new SaveWorkstationConfigRequest(jsonContent);
//        var response = await _client.PostAsJsonAsync("/api/workstation-config", request);
//        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
//        return (response.StatusCode, content);
//    }


//    // =====================================
//    // 辅助方法：获取基础的合法 JSON
//    // =====================================
//    private string GetBaseValidJson()
//    {
//        return """
//        {
//            "Id": "WS-001",
//            "IpAddress": "192.168.1.100",
//            "Protocols": [
//                {
//                    "Id": "P-001",
//                    "ProtocolType": 0, 
//                    "IpAddress": "127.0.0.1",
//                    "ProtocolPort": 502,
//                    "InterfaceType": 0,
//                    "Equipments": [
//                        {
//                            "Id": "E-001",
//                            "ProtocolType": 0,
//                            "EquipmentType": 1,
//                            "Parameters": [
//                                {
//                                    "Label": "Temperature",
//                                    "Address": "40001",
//                                    "StationNo": "1",
//                                    "DataFormat": 1,
//                                    "ProtocolType": 0,
//                                    "EquipmentId": "E-001",
//                                    "DataType": 1,
//                                    "AddressStartWithZero": true
//                                }
//                            ]
//                        }
//                    ]
//                }
//            ]
//        }
//        """;
//    } 
//    #endregion

//    // =====================================
//    // 正常流程测试
//    // =====================================
//    [Fact(DisplayName = "正常流程测试(200 Ok)")]
//    public async Task SaveConfig_WithValidRequest_ReturnsOk()
//    {
//        var validJson = GetBaseValidJson();
//        var (statusCode, content) = await PostConfigAsync(validJson);

//        if (statusCode == HttpStatusCode.BadRequest)
//        {
//            var msg = content.GetProperty("message").GetString();
//            throw new Exception($"正常用例构造失败，触发了业务验证: {msg}");
//        }

//        statusCode.Should().Be(HttpStatusCode.OK);
//        content.GetProperty("success").GetBoolean().Should().BeTrue();
//        content.GetProperty("message").GetString().Should().Be("配置保存成功");
//    }

//    // =====================================
//    // 工作站 (Workstation) 校验失败测试
//    // =====================================
//    [Fact(DisplayName = "工作站校验失败 - Id为空")]
//    public async Task SaveConfig_WorkstationEmptyId_ReturnsBadRequest()
//    {
//        var node = JsonNode.Parse(GetBaseValidJson())!;
//        node["Id"] = ""; // 触发 Id NotEmpty 校验

//        var (statusCode, content) = await PostConfigAsync(node.ToJsonString());

//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//        content.GetProperty("title").GetString().Should().Be("数据验证失败");

//        // 验证 errors 字典中包含 Id 键
//        var errors = content.GetProperty("errors");
//        errors.TryGetProperty("Id", out _).Should().BeTrue();
//    }

//    [Fact(DisplayName = "工作站校验失败 - IP地址格式无效")]
//    public async Task SaveConfig_WorkstationInvalidIp_ReturnsBadRequest()
//    {
//        var node = JsonNode.Parse(GetBaseValidJson())!;
//        node["IpAddress"] = "999.999.999.999"; // 触发 IP 格式校验

//        var (statusCode, content) = await PostConfigAsync(node.ToJsonString());

//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//        var errors = content.GetProperty("errors");
//        errors.TryGetProperty("IpAddress", out _).Should().BeTrue();
//    }

//    // =====================================
//    // 协议 (Protocol) 校验失败测试
//    // =====================================
//    [Fact(DisplayName = "协议校验失败 - 设备列表为空")]
//    public async Task SaveConfig_ProtocolEmptyEquipments_ReturnsBadRequest()
//    {
//        var node = JsonNode.Parse(GetBaseValidJson())!;
//        node["Protocols"]![0]!["Equipments"]!.AsArray().Clear(); // 触发 Equipments NotEmpty 校验

//        var (statusCode, content) = await PostConfigAsync(node.ToJsonString());

//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//        var errors = content.GetProperty("errors");
//        // FluentValidation 集合验证的属性名通常是 Protocols[0].Equipments
//        errors.EnumerateObject().Should().Contain(e => e.Name.Contains("Equipments"));
//    }

//    [Fact(DisplayName = "协议校验失败 - 网口端口号越界")]
//    public async Task SaveConfig_ProtocolInvalidPort_ReturnsBadRequest()
//    {
//        var node = JsonNode.Parse(GetBaseValidJson())!;
//        node["Protocols"]![0]!["ProtocolPort"] = 70000; // 触发 ProtocolPort <= 65535 校验

//        var (statusCode, content) = await PostConfigAsync(node.ToJsonString());

//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//        var errors = content.GetProperty("errors");
//        errors.EnumerateObject().Should().Contain(e => e.Name.Contains("ProtocolPort"));
//    }

//    // =====================================
//    // 设备 (Equipment) 校验失败测试
//    // =====================================
//    [Fact(DisplayName = "设备校验失败 - Id为空")]
//    public async Task SaveConfig_EquipmentEmptyId_ReturnsBadRequest()
//    {
//        var node = JsonNode.Parse(GetBaseValidJson())!;
//        node["Protocols"]![0]!["Equipments"]![0]!["Id"] = ""; // 触发 Equipment Id NotEmpty 校验

//        var (statusCode, content) = await PostConfigAsync(node.ToJsonString());

//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//        var errors = content.GetProperty("errors");
//        errors.EnumerateObject().Should().Contain(e => e.Name.Contains("Id") && e.Name.Contains("Equipments"));
//    }

//    [Fact(DisplayName = "设备校验失败 - 参数列表为空")]
//    public async Task SaveConfig_EquipmentEmptyParameters_ReturnsBadRequest()
//    {
//        var node = JsonNode.Parse(GetBaseValidJson())!;
//        node["Protocols"]![0]!["Equipments"]![0]!["Parameters"]!.AsArray().Clear(); // 触发 Parameters NotEmpty 校验

//        var (statusCode, content) = await PostConfigAsync(node.ToJsonString());

//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//        var errors = content.GetProperty("errors");
//        errors.EnumerateObject().Should().Contain(e => e.Name.Contains("Parameters"));
//    }

//    // =====================================
//    // 参数 (Parameter) 校验失败测试
//    // =====================================
//    [Fact(DisplayName = "参数校验失败 - 标签(Label)为空")]
//    public async Task SaveConfig_ParameterEmptyLabel_ReturnsBadRequest()
//    {
//        var node = JsonNode.Parse(GetBaseValidJson())!;
//        node["Protocols"]![0]!["Equipments"]![0]!["Parameters"]![0]!["Label"] = ""; // 触发 Label NotEmpty 校验

//        var (statusCode, content) = await PostConfigAsync(node.ToJsonString());

//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//        var errors = content.GetProperty("errors");
//        errors.EnumerateObject().Should().Contain(e => e.Name.Contains("Label"));
//    }

//    [Fact(DisplayName = "参数校验失败 - 最小值大于最大值")]
//    public async Task SaveConfig_ParameterMinGreaterThanMax_ReturnsBadRequest()
//    {
//        var node = JsonNode.Parse(GetBaseValidJson())!;
//        // 注入 MinValue 和 MaxValue 触发自定义逻辑校验
//        node["Protocols"]![0]!["Equipments"]![0]!["Parameters"]![0]!["MinValue"] = "100";
//        node["Protocols"]![0]!["Equipments"]![0]!["Parameters"]![0]!["MaxValue"] = "50";

//        var (statusCode, content) = await PostConfigAsync(node.ToJsonString());

//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//        var errors = content.GetProperty("errors");
//        errors.EnumerateObject().Should().Contain(e => e.Name.Contains("MaxValue"));
//    }

//    // =====================================
//    // 异常与边界情况测试 (400 BadRequest 等)
//    // =====================================

//    [Fact(DisplayName = "异常处理测试 - JSON格式严重错误导致反序列化失败(400)")]
//    public async Task SaveConfig_InvalidJsonFormat_ReturnsBadRequest()
//    {
//        // 构造一个格式被破坏的 JSON（例如缺少闭合括号）
//        var invalidJson = """
//        {
//            "Id": "WS-001",
//            "IpAddress": "192.168.1.100"
//        """; // 缺少闭合的 }

//        var (statusCode, content) = await PostConfigAsync(invalidJson);

//        // 业务层抛出 ArgumentException，GlobalExceptionHandler 捕获后应返回 400
//        statusCode.Should().Be(HttpStatusCode.BadRequest);
//        content.GetProperty("title").GetString().Should().Be("参数错误");
//        content.GetProperty("detail").GetString().Should().Contain("配置格式错误");
//    }

//    [Fact(DisplayName = "异常处理测试 - JSON内容为空字符串(400)")]
//    public async Task SaveConfig_EmptyJsonContent_ReturnsBadRequest()
//    {
//        // 传入空字符串，触发 DTO 上的 [Required] 模型验证拦截
//        var (statusCode, content) = await PostConfigAsync("   ");

//        statusCode.Should().Be(HttpStatusCode.BadRequest);

//        // 框架自带的 400 响应格式是 ProblemDetails，包含 errors 字段
//        content.TryGetProperty("errors", out var errors).Should().BeTrue();
//        var errorString = errors.ToString();
//        errorString.Should().Contain("不能为空");
//    }

//    [Fact(DisplayName = "边界情况测试 - 请求体完全为空(400/415)")]
//    public async Task SaveConfig_NullRequest_ReturnsBadRequest()
//    {
//        // 不使用 PostConfigAsync 辅助方法，直接发送一个没有 Body 的 POST 请求
//        var response = await _client.PostAsync("/api/workstation-config", null);

//        // 框架默认会拦截并返回 400 Bad Request 或 415 Unsupported Media Type
//        response.IsSuccessStatusCode.Should().BeFalse();
//        response.StatusCode.Should().Match(c => c == HttpStatusCode.BadRequest || c == HttpStatusCode.UnsupportedMediaType);
//    }

//    // =====================================
//    // 500 内部服务器错误测试
//    // =====================================
//    [Fact(DisplayName = "异常处理测试 - 模拟服务器内部未知异常(500)")]
//    public async Task SaveConfig_InternalError_ReturnsInternalServerError()
//    {
//        // 1. 创建一个 Mock 的仓储，强制让它在被调用时抛出 Exception
//        var mockRepo = new Mock<IWorkstationConfigEntityRepository>();
//        mockRepo.Setup(x => x.AddAsync(It.IsAny<IndustrialDataProcessor.Domain.Entities.WorkstationConfigEntity>(), It.IsAny<CancellationToken>()))
//                .ThrowsAsync(new Exception("模拟的数据库连接超时或崩溃"));

//        // 2. 使用 WithWebHostBuilder 替换掉真实的仓储
//        var client = _factory.WithWebHostBuilder(builder =>
//        {
//            builder.ConfigureServices(services =>
//            {
//                // 移除真实的 IWorkstationConfigRepository
//                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IWorkstationConfigEntityRepository));
//                if (descriptor != null)
//                {
//                    services.Remove(descriptor);
//                }

//                // 注入我们 Mock 的必定抛出异常的仓储
//                services.AddScoped(_ => mockRepo.Object);
//            });
//        }).CreateClient();

//        // 3. 发送一个完全合法的请求
//        var validJson = GetBaseValidJson();
//        var request = new SaveWorkstationConfigRequest(validJson);
//        var response = await client.PostAsJsonAsync("/api/workstation-config", request);

//        // 4. 验证是否返回了 500
//        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

//        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

//        // 验证 GlobalExceptionHandler 返回的 500 格式
//        content.GetProperty("status").GetInt32().Should().Be(500);
//        content.GetProperty("title").GetString().Should().Be("服务器内部错误");
//    }
//}
