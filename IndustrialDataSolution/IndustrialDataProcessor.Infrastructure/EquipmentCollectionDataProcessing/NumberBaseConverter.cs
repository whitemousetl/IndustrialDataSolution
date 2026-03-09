using System.Globalization;

namespace IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;

/// <summary>
/// 数值进制转换器
/// 支持: 十六进制 ↔ 十进制
/// </summary>
public static class NumberBaseConverter
{
    /// <summary>
    /// 十六进制转十进制
    /// 支持格式: "FF", "0xFF", "ff"
    /// </summary>
    public static long HexToDecimal(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string hexString = value.ToString()?.Trim() ?? "";

        // 移除 0x 前缀
        if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hexString = hexString[2..];

        if (long.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long result))
            return result;

        throw new FormatException($"无法将值 '{value}' 解析为十六进制数");
    }

    /// <summary>
    /// 十进制转十六进制
    /// </summary>
    /// <param name="value">十进制值</param>
    /// <param name="withPrefix">是否添加 0x 前缀</param>
    /// <returns>十六进制字符串</returns>
    public static string DecimalToHex(object? value, bool withPrefix = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        string str = value.ToString()?.Trim() ?? "";

        if (!long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimalValue))
            throw new FormatException($"无法将值 '{value}' 解析为十进制数");

        string hex = decimalValue.ToString("X");
        return withPrefix ? $"0x{hex}" : hex;
    }
}
