namespace IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;

/// <summary>
/// 数值类型检查工具
/// </summary>
public static class NumericTypeChecker
{
    /// <summary>
    /// 判断对象是否为数值类型
    /// 支持: 所有数值原生类型、JsonElement、可解析的数值字符串
    /// </summary>
    public static bool IsNumeric(object? value)
    {
        if (value == null)
            return false;

        // 原生数值类型
        if (value is sbyte or byte or short or ushort or int or uint
            or long or ulong or float or double or decimal)
            return true;

        // JsonElement 数值
        if (value is System.Text.Json.JsonElement je
            && je.ValueKind == System.Text.Json.JsonValueKind.Number)
            return true;

        // 可解析的数值字符串
        if (value is string s && double.TryParse(s, out _))
            return true;

        return false;
    }

    /// <summary>
    /// 尝试将对象转换为 double
    /// </summary>
    //public static bool TryConvertToDouble(object? value, out double result)
    //{
    //    result = 0;

    //    if (value == null)
    //        return false;

    //    try
    //    {
    //        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
    //        {
    //            result = je.GetDouble();
    //            return true;
    //        }

    //        result = Convert.ToDouble(value);
    //        return true;
    //    }
    //    catch
    //    {
    //        return false;
    //    }
    //}
}
