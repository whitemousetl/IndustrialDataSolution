using System.Text.Json;

namespace IndustrialDataProcessor.Infrastructure.Helpers;

public static class JsonValidateHelper
{
    public static T EnsurePropertyExistsAndTypeIsRight<T>(JsonElement prop, string namePrefix, string name, JsonValueKind expectedKind)
    {
        ValidateEnsurePropertyExistsAndTypeIsRightParams<T>(prop, name, namePrefix, expectedKind);

        var msgPrefix = namePrefix + name;

        var propJsonElement = GetRequiredProperty(prop, name, msgPrefix);

        ValidateJsonElementNotNullOrUndefined(propJsonElement, msgPrefix);

        return EnsurePropertyTypeIsRight<T>(propJsonElement, msgPrefix, expectedKind);
    }

    public static TEnum EnsurePropertyExistsAndEnumIsRight<TEnum>(JsonElement prop, string namePrefix, string name) where TEnum : struct, Enum
    {
        ValidateEnsurePropertyExistsAndTypeIsRightParams(prop, name, namePrefix);

        var msgPrefix = namePrefix + name;

        if (!prop.TryGetProperty(name, out var propJsonElement))
            throw new JsonException($"{msgPrefix}字段缺少");

        // 类型校验：必须为数字
        if (propJsonElement.ValueKind != JsonValueKind.Number)
            throw new JsonException($"{msgPrefix}字段必须为数字");

        return EnsureEnumIsRight<TEnum>(propJsonElement, msgPrefix);
    }

    public static T? ValidateOptionalFields<T>(JsonElement root, string namePrefix, string name, JsonValueKind expectedKind)
    {
        ValidateEnsurePropertyExistsAndTypeIsRightParams<T>(root, name, namePrefix, expectedKind);

        var msgPrefix = namePrefix + name;

        if (!root.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null || prop.ValueKind == JsonValueKind.Undefined)
            return default;
        else
            return EnsurePropertyTypeIsRight<T>(prop, msgPrefix, expectedKind);
    }

    public static void ValidateOptionalFields(JsonElement root, string namePrefix, params (string Name, JsonValueKind Kind)[] fields)
    {
        if (fields == null)
            throw new ArgumentNullException(nameof(fields), "ValidateOptionalFields方法，fields参数不能为null");

        foreach (var (name, expectedKind) in fields)
        {
            ValidateEnsurePropertyExistsAndTypeIsRightParams(root, name, namePrefix, expectedKind);

            var msgPrefix = namePrefix + name;

            if (root.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
                ValidatePropertyKind(prop, expectedKind, msgPrefix);
        }
    }

    public static TEnum? GetOptionalEnum<TEnum>(JsonElement root, string namePrefix, string name)
        where TEnum : struct, Enum
    {
        ValidateEnsurePropertyExistsAndTypeIsRightParams(root, name, namePrefix);

        var msgPrefix = namePrefix + name;

        // 字段不存在或为null，返回null
        if (!root.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
            return null;

        if (prop.ValueKind != JsonValueKind.Number)
            throw new JsonException($"{msgPrefix}字段存在时，必须为数字类型");

        int value;
        try
        {
            value = prop.GetInt32();
        }
        catch (Exception ex)
        {
            throw new JsonException($"{msgPrefix}字段存在时，无法转换为数字( -2,147,483,648 到 2,147,483,647)", ex);
        }

        if (!Enum.IsDefined(typeof(TEnum), value))
            throw new JsonException($"{msgPrefix}值{value}超出有效范围");

        return (TEnum)(object)value;
    }

    private static T EnsurePropertyTypeIsRight<T>(JsonElement prop, string msgPrefix, JsonValueKind expectedKind)
    {
        // 验证 JSON 类型是否匹配
        ValidatePropertyKind(prop, expectedKind, msgPrefix);

        // 直接返回 JsonElement
        if (typeof(T) == typeof(JsonElement))
            return (T)(object)prop;

        // 反序列化并返回值
        var value = DeserializeValue<T>(prop, expectedKind, msgPrefix);

        return value;
    }

    private static void ValidatePropertyKind(JsonElement prop, JsonValueKind expectedKind, string msgPrefix)
    {
        if (IsBooleanKind(expectedKind))
        {
            if (!IsBooleanKind(prop.ValueKind))
                throw new JsonException($"{msgPrefix}字段必须为布尔类型");
        }
        else if (prop.ValueKind != expectedKind)
        {
            var typeName = GetTypeName(expectedKind);
            throw new JsonException($"{msgPrefix}字段必须为{typeName}");
        }
    }

    private static bool IsBooleanKind(JsonValueKind kind) =>
        kind == JsonValueKind.True || kind == JsonValueKind.False;

    private static string GetTypeName(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Object => "对象",
        JsonValueKind.String => "字符串",
        JsonValueKind.Number => "数字",
        JsonValueKind.Array => "数组",
        _ => "未知类型"
    };

    private static T DeserializeValue<T>(JsonElement prop, JsonValueKind expectedKind, string msgPrefix)
    {
        return expectedKind switch
        {
            JsonValueKind.String => (T)(object)prop.GetString()!,
            JsonValueKind.Number => DeserializeNumber<T>(prop, msgPrefix),
            JsonValueKind.True or JsonValueKind.False => (T)(object)prop.GetBoolean(),
            JsonValueKind.Object or JsonValueKind.Array => JsonSerializer.Deserialize<T>(prop, JsonOptionsProvider.WorkstationJsonOptions)!,
            _ => throw new JsonException($"{msgPrefix}字段类型不支持")
        };
    }

    private static T DeserializeNumber<T>(JsonElement prop, string msgPrefix)
    {
        if (typeof(T) == typeof(int)) return (T)(object)prop.GetInt32();
        if (typeof(T) == typeof(long)) return (T)(object)prop.GetInt64();
        if (typeof(T) == typeof(double)) return (T)(object)prop.GetDouble();
        if (typeof(T) == typeof(float)) return (T)(object)prop.GetSingle();
        if (typeof(T) == typeof(short)) return (T)(object)prop.GetInt16();
        if (typeof(T) == typeof(uint)) return (T)(object)prop.GetUInt32();
        if (typeof(T) == typeof(ulong)) return (T)(object)prop.GetUInt64();
        if (typeof(T) == typeof(ushort)) return (T)(object)prop.GetUInt16();
        if (typeof(T) == typeof(int?)) return (T)(object)prop.GetInt32();
        if (typeof(T) == typeof(long?)) return (T)(object)prop.GetInt64();
        if (typeof(T) == typeof(double?)) return (T)(object)prop.GetDouble();
        if (typeof(T) == typeof(float?)) return (T)(object)prop.GetSingle();
        if (typeof(T) == typeof(short?)) return (T)(object)prop.GetInt16();
        if (typeof(T) == typeof(uint?)) return (T)(object)prop.GetUInt32();
        if (typeof(T) == typeof(ulong?)) return (T)(object)prop.GetUInt64();
        if (typeof(T) == typeof(ushort?)) return (T)(object)prop.GetUInt16();

        throw new JsonException($"{msgPrefix}字段为数字，但泛型T不支持");
    }

    private static TEnum EnsureEnumIsRight<TEnum>(JsonElement prop, string msgPrefix) where TEnum : struct, Enum
    {
        int value;
        try
        {
            value = prop.GetInt32();
        }
        catch (Exception ex)
        {
            throw new JsonException($"{msgPrefix}字段无法转换为数字", ex);
        }

        // 枚举范围校验
        if (!Enum.IsDefined(typeof(TEnum), value))
            throw new JsonException($"{msgPrefix}值{value}超出有效范围");

        return (TEnum)(object)value;
    }

    private static void ValidateEnsurePropertyExistsAndTypeIsRightParams<T>(JsonElement prop, string name, string namePrefix, JsonValueKind expectedKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(namePrefix);

        if (prop.ValueKind == JsonValueKind.Null)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数prop不能为JsonValueKind.Null");
        if (prop.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数prop不能为JsonValueKind.Undefined");
        if (expectedKind == JsonValueKind.Null)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数expectedKind不能为JsonValueKind.Null");
        if (expectedKind == JsonValueKind.Undefined)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数expectedKind不能为JsonValueKind.Undefined");

        var supportedTypes = new[]
        {
            typeof(short?),typeof(int?), typeof(long?), typeof(ushort?),typeof(uint?), typeof(ulong?), typeof(double?), typeof(float?),
            typeof(short),typeof(int), typeof(long), typeof(ushort),typeof(uint), typeof(ulong), typeof(double), typeof(float),
            typeof(string), typeof(bool), typeof(bool?),typeof(JsonElement),
        };
        var tType = typeof(T);
        bool isSupported =
            supportedTypes.Contains(tType) ||
            tType.IsClass || tType.IsArray;
        if (!isSupported)
            throw new NotSupportedException($"ValidateEnsurePropertyExistsAndTypeIsRightParams方法不支持类型:  {tType.FullName}");
    }

    private static void ValidateEnsurePropertyExistsAndTypeIsRightParams(JsonElement prop, string name, string namePrefix, JsonValueKind expectedKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(namePrefix);

        if (prop.ValueKind == JsonValueKind.Null)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数prop不能为JsonValueKind.Null");
        if (prop.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数prop不能为JsonValueKind.Undefined");
        if (expectedKind == JsonValueKind.Null)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数expectedKind不能为JsonValueKind.Null");
        if (expectedKind == JsonValueKind.Undefined)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数expectedKind不能为JsonValueKind.Undefined");
    }

    private static void ValidateEnsurePropertyExistsAndTypeIsRightParams(JsonElement prop, string name, string namePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(namePrefix);

        if (prop.ValueKind == JsonValueKind.Null)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数prop不能为JsonValueKind.Null");
        if (prop.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("ValidateEnsurePropertyExistsAndTypeIsRightParams方法，JsonElement类型的参数prop不能为JsonValueKind.Undefined");
    }

    private static JsonElement GetRequiredProperty(JsonElement prop, string name, string msgPrefix)
    {
        if (!prop.TryGetProperty(name, out var propJsonElement))
            throw new JsonException($"{msgPrefix}字段缺少");
        return propJsonElement;
    }

    private static void ValidateJsonElementNotNullOrUndefined(JsonElement propJsonElement, string msgPrefix)
    {
        if (propJsonElement.ValueKind == JsonValueKind.Null)
            throw new JsonException($"{msgPrefix}，根据JsonElement类型的root获得的返回值propJsonElement，JsonElement类型不能为JsonValueKind.Null");
        if (propJsonElement.ValueKind == JsonValueKind.Undefined)
            throw new JsonException($"{msgPrefix}，根据JsonElement类型的root获得的返回值propJsonElement，JsonElement类型不能为JsonValueKind.Undefined");
    }
}