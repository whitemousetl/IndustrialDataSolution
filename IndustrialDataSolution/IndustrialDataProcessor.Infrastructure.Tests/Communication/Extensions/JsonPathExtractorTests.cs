using FluentAssertions;
using IndustrialDataProcessor.Infrastructure.Communication.Extensions;

namespace IndustrialDataProcessor.Infrastructure.Tests.Communication.Extensions;

/// <summary>
/// JsonPathExtractor 单元测试
/// 测试从JSON中按路径提取值的功能
/// </summary>
public class JsonPathExtractorTests
{
    /// <summary>
    /// 测试用的完整JSON数据
    /// </summary>
    private const string TestJson = """
        {
            "store": {
                "book": [
                    {
                        "category": "reference",
                        "author": "Nigel Rees",
                        "title": "Sayings of the Century",
                        "price": 8.95
                    },
                    {
                        "category": "fiction",
                        "author": "Evelyn Waugh",
                        "title": "Sword of Honour",
                        "price": 12.99
                    },
                    {
                        "category": "fiction",
                        "author": "Herman Melville",
                        "title": "Moby Dick",
                        "isbn": "0-553-21311-3",
                        "price": 8.99
                    }
                ],
                "bicycle": {
                    "color": "red",
                    "price": 19.95,
                    "available": true
                }
            },
            "name": "Test Store",
            "count": 100,
            "isOpen": false,
            "rating": 4.5,
            "tags": ["electronics", "books", "bicycles"],
            "metadata": null
        }
        """;

    #region 基本对象属性访问测试

    [Fact]
    public void ExtractValue_SimpleProperty_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "name");

        // Assert
        result.Should().Be("Test Store");
    }

    [Fact]
    public void ExtractValue_NestedObjectProperty_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.bicycle.color");

        // Assert
        result.Should().Be("red");
    }

    [Fact]
    public void ExtractValue_DeeplyNestedProperty_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.bicycle.price");

        // Assert
        result.Should().Be(19.95);
    }

    #endregion

    #region 数组索引访问测试

    [Fact]
    public void ExtractValue_ArrayFirstElement_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.book[0].title");

        // Assert
        result.Should().Be("Sayings of the Century");
    }

    [Fact]
    public void ExtractValue_ArraySecondElement_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.book[1].title");

        // Assert
        result.Should().Be("Sword of Honour");
    }

    [Fact]
    public void ExtractValue_ArrayThirdElement_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.book[2].author");

        // Assert
        result.Should().Be("Herman Melville");
    }

    [Fact]
    public void ExtractValue_ArrayElementNestedProperty_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.book[0].price");

        // Assert
        result.Should().Be(8.95);
    }

    [Fact]
    public void ExtractValue_SimpleArray_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "tags[0]");

        // Assert
        result.Should().Be("electronics");
    }

    [Fact]
    public void ExtractValue_SimpleArraySecondElement_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "tags[1]");

        // Assert
        result.Should().Be("books");
    }

    #endregion

    #region 不存在路径处理测试

    [Fact]
    public void ExtractValue_NonExistentProperty_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_NonExistentNestedProperty_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.nonexistent.property");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_ArrayIndexOutOfBounds_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.book[99].title");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_NegativeArrayIndex_ReturnsNull()
    {
        // Arrange & Act - 负数索引的正则不会匹配，所以会被当作普通属性处理
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.book[-1].title");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_PropertyOnNonObject_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "name.nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region 不同类型值的提取测试

    [Fact]
    public void ExtractValue_StringValue_ReturnsString()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "name");

        // Assert
        result.Should().BeOfType<string>();
        result.Should().Be("Test Store");
    }

    [Fact]
    public void ExtractValue_IntegerValue_ReturnsInt32()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "count");

        // Assert
        result.Should().BeOfType<int>();
        result.Should().Be(100);
    }

    [Fact]
    public void ExtractValue_DoubleValue_ReturnsDouble()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "rating");

        // Assert
        result.Should().BeOfType<double>();
        result.Should().Be(4.5);
    }

    [Fact]
    public void ExtractValue_BooleanTrue_ReturnsTrue()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.bicycle.available");

        // Assert
        result.Should().BeOfType<bool>();
        result.Should().Be(true);
    }

    [Fact]
    public void ExtractValue_BooleanFalse_ReturnsFalse()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "isOpen");

        // Assert
        result.Should().BeOfType<bool>();
        result.Should().Be(false);
    }

    [Fact]
    public void ExtractValue_NullValue_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "metadata");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_ArrayValue_ReturnsJsonString()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "tags");

        // Assert
        result.Should().BeOfType<string>();
        result!.ToString().Should().Contain("electronics");
    }

    [Fact]
    public void ExtractValue_ObjectValue_ReturnsJsonString()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "store.bicycle");

        // Assert
        result.Should().BeOfType<string>();
        result!.ToString().Should().Contain("color");
        result.ToString().Should().Contain("red");
    }

    #endregion

    #region 泛型类型转换测试

    [Fact]
    public void ExtractValueGeneric_ConvertToString_ReturnsString()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue<string>(TestJson, "name");

        // Assert
        result.Should().Be("Test Store");
    }

    [Fact]
    public void ExtractValueGeneric_ConvertToInt_ReturnsInt()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue<int>(TestJson, "count");

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void ExtractValueGeneric_ConvertToDouble_ReturnsDouble()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue<double>(TestJson, "rating");

        // Assert
        result.Should().Be(4.5);
    }

    [Fact]
    public void ExtractValueGeneric_ConvertToBool_ReturnsBool()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue<bool>(TestJson, "isOpen");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExtractValueGeneric_InvalidConversion_ReturnsDefault()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue<int>(TestJson, "name");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void ExtractValueGeneric_NonExistentPath_ReturnsDefault()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue<string>(TestJson, "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void ExtractValue_EmptyJson_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue("", "name");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_NullJson_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(null!, "name");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_EmptyPath_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_NullPath_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_InvalidJson_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue("{ invalid json }", "name");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_WhitespaceOnlyPath_ReturnsNull()
    {
        // Arrange & Act
        var result = JsonPathExtractor.ExtractValue(TestJson, "   ");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region 条件筛选取值测试（根数组场景）

    /// <summary>
    /// 根级数组 JSON，用于测试条件筛选功能
    /// 模拟上一轮需求：API 直接返回 StationDetails 数组
    /// </summary>
    private const string RootArrayJson = """
        [
            {"StationCode": 0, "StationName": "Machine",   "Detail": "CurrentOperationOrderRowId", "Value": "0"},
            {"StationCode": 1, "StationName": "In station", "Detail": "CurrentOperationOrderRowId", "Value": "0"},
            {"StationCode": 20,"StationName": "Line 1",    "Detail": "ChangeProfileStatus",        "Value": "2"},
            {"StationCode": 0, "StationName": "Machine",   "Detail": "CurrentStationStatus",       "Value": "1008"},
            {"StationCode": 0, "StationName": "Machine",   "Detail": "Uptime",                     "Value": "False"}
        ]
        """;

    [Fact]
    public void ExtractValue_RootArray_SingleCondition_ReturnsCorrectValue()
    {
        // Arrange & Act - 单条件筛选
        var result = JsonPathExtractor.ExtractValue(RootArrayJson, "[?Detail=Uptime].Value");

        // Assert
        result.Should().Be("False");
    }

    [Fact]
    public void ExtractValue_RootArray_MultipleConditions_ReturnsCorrectValue()
    {
        // Arrange & Act - 三条件组合筛选（包含含空格的值 "Line 1"）
        var result = JsonPathExtractor.ExtractValue(
            RootArrayJson,
            "[?StationCode=20&StationName=Line 1&Detail=ChangeProfileStatus].Value");

        // Assert
        result.Should().Be("2");
    }

    [Fact]
    public void ExtractValue_RootArray_NumericCondition_ReturnsCorrectValue()
    {
        // Arrange & Act - 数字条件（JSON中是数字类型，条件值是字符串，应能匹配）
        var result = JsonPathExtractor.ExtractValue(
            RootArrayJson,
            "[?StationCode=0&Detail=CurrentStationStatus].Value");

        // Assert
        result.Should().Be("1008");
    }

    [Fact]
    public void ExtractValue_RootArray_ConditionNoMatch_ReturnsNull()
    {
        // Arrange & Act - 条件不匹配任何元素
        var result = JsonPathExtractor.ExtractValue(
            RootArrayJson,
            "[?StationCode=99&Detail=NonExistent].Value");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_RootArray_ConditionMatchesButFieldMissing_ReturnsNull()
    {
        // Arrange & Act - 条件匹配但目标字段不存在
        var result = JsonPathExtractor.ExtractValue(
            RootArrayJson,
            "[?StationCode=20&Detail=ChangeProfileStatus].NonExistentField");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_RootArray_CaseInsensitiveConditionKey_ReturnsCorrectValue()
    {
        // Arrange & Act - 条件键大小写不敏感
        var result = JsonPathExtractor.ExtractValue(
            RootArrayJson,
            "[?detail=Uptime].Value");

        // Assert
        result.Should().Be("False");
    }

    #endregion

    #region 条件筛选取值测试（嵌套对象场景）

    /// <summary>
    /// 嵌套对象 JSON，模拟 Document.Body.StationDetails 结构
    /// </summary>
    private const string NestedDocumentJson = """
        {
            "Document": {
                "Header": {
                    "Sender": "PrimeConnector",
                    "Receiver": "ERP",
                    "MessageID": "450205c7-5a63-4c38-a971-cd5738b74aae"
                },
                "Body": {
                    "StationDetails": [
                        {"StationCode": 0,  "StationName": "Machine",  "Detail": "CurrentOperationOrderRowId", "Value": "0"},
                        {"StationCode": 1,  "StationName": "In station","Detail": "CurrentOperationOrderRowId", "Value": "5"},
                        {"StationCode": 20, "StationName": "Line 1",   "Detail": "ChangeProfileStatus",        "Value": "2"},
                        {"StationCode": 21, "StationName": "Line 2",   "Detail": "ChangeProfileStatus",        "Value": "3"},
                        {"StationCode": 0,  "StationName": "Machine",  "Detail": "Uptime",                     "Value": "True"}
                    ]
                }
            }
        }
        """;

    [Fact]
    public void ExtractValue_NestedDocument_ConditionalOnNestedArray_ReturnsCorrectValue()
    {
        // Arrange & Act - 嵌套路径 + 条件筛选（3个条件，含空格值）
        var result = JsonPathExtractor.ExtractValue(
            NestedDocumentJson,
            "Document.Body.StationDetails.[?StationCode=20&StationName=Line 1&Detail=ChangeProfileStatus].Value");

        // Assert
        result.Should().Be("2");
    }

    [Fact]
    public void ExtractValue_NestedDocument_DifferentRowSameConditionKey_ReturnsCorrectRow()
    {
        // Arrange & Act - Line 1 和 Line 2 同样有 ChangeProfileStatus，验证 StationCode 条件能区分
        var line2Value = JsonPathExtractor.ExtractValue(
            NestedDocumentJson,
            "Document.Body.StationDetails.[?StationCode=21&StationName=Line 2&Detail=ChangeProfileStatus].Value");

        // Assert
        line2Value.Should().Be("3");
    }

    [Fact]
    public void ExtractValue_NestedDocument_SingleConditionOnNestedArray_ReturnsCorrectValue()
    {
        // Arrange & Act - 只用一个条件（Detail=Uptime 在整个数组中唯一）
        var result = JsonPathExtractor.ExtractValue(
            NestedDocumentJson,
            "Document.Body.StationDetails.[?Detail=Uptime].Value");

        // Assert
        result.Should().Be("True");
    }

    [Fact]
    public void ExtractValue_NestedDocument_HeaderProperty_WithPositionMode_ReturnsCorrectValue()
    {
        // Arrange & Act - 验证同一 JSON 中位置取值（嵌套对象属性）仍然正常工作
        var sender = JsonPathExtractor.ExtractValue(NestedDocumentJson, "Document.Header.Sender");
        var messageId = JsonPathExtractor.ExtractValue(NestedDocumentJson, "Document.Header.MessageID");

        // Assert
        sender.Should().Be("PrimeConnector");
        messageId.Should().Be("450205c7-5a63-4c38-a971-cd5738b74aae");
    }

    [Fact]
    public void ExtractValue_NestedDocument_PositionAccessOnNestedArray_ReturnsCorrectValue()
    {
        // Arrange & Act - 嵌套数组中使用下标取值（位置模式）
        var result = JsonPathExtractor.ExtractValue(
            NestedDocumentJson,
            "Document.Body.StationDetails[2].Value");

        // Assert - 下标 2 对应 Line 1 的 ChangeProfileStatus
        result.Should().Be("2");
    }

    [Fact]
    public void ExtractValue_NestedDocument_ConditionNoMatch_ReturnsNull()
    {
        // Arrange & Act - 嵌套路径下条件不匹配
        var result = JsonPathExtractor.ExtractValue(
            NestedDocumentJson,
            "Document.Body.StationDetails.[?StationCode=99].Value");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractValue_NestedDocument_ConditionalOnNonArray_ReturnsNull()
    {
        // Arrange & Act - 对非数组节点使用条件筛选，应返回 null
        var result = JsonPathExtractor.ExtractValue(
            NestedDocumentJson,
            "Document.Header.[?Sender=PrimeConnector].Receiver");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region 工业数据采集场景测试

    [Fact]
    public void ExtractValue_IndustrialApiResponse_ExtractsTemperature()
    {
        // Arrange - 模拟工业API返回数据
        var industrialJson = """
            {
                "deviceId": "DEVICE-001",
                "timestamp": "2024-01-15T10:30:00Z",
                "sensors": {
                    "temperature": {
                        "value": 25.5,
                        "unit": "°C",
                        "status": "normal"
                    },
                    "pressure": {
                        "value": 101.325,
                        "unit": "kPa",
                        "status": "normal"
                    }
                },
                "alarms": []
            }
            """;

        // Act
        var temperature = JsonPathExtractor.ExtractValue(industrialJson, "sensors.temperature.value");
        var unit = JsonPathExtractor.ExtractValue(industrialJson, "sensors.temperature.unit");

        // Assert
        temperature.Should().Be(25.5);
        unit.Should().Be("°C");
    }

    [Fact]
    public void ExtractValue_IndustrialApiResponse_ExtractsArrayData()
    {
        // Arrange - 模拟带数组的工业API返回数据
        var industrialJson = """
            {
                "deviceId": "LINE-001",
                "workstations": [
                    {
                        "id": "WS-001",
                        "status": "running",
                        "output": 150
                    },
                    {
                        "id": "WS-002",
                        "status": "idle",
                        "output": 0
                    }
                ]
            }
            """;

        // Act
        var firstStationStatus = JsonPathExtractor.ExtractValue(industrialJson, "workstations[0].status");
        var secondStationOutput = JsonPathExtractor.ExtractValue(industrialJson, "workstations[1].output");

        // Assert
        firstStationStatus.Should().Be("running");
        secondStationOutput.Should().Be(0);
    }

    #endregion
}
