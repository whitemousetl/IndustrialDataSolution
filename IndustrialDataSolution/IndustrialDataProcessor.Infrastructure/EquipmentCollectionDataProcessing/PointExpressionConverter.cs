using IndustrialDataProcessor.Domain.Workstation.Configs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;

public class PointExpressionConverter 
{
    private readonly ILogger<PointExpressionConverter> _logger;

    public PointExpressionConverter(ILogger<PointExpressionConverter> logger)
    {
        _logger = logger;
    }

    public object? Convert(ParameterConfig point, object? value)
    {
        try
        {
            if (string.IsNullOrEmpty(point.PositiveExpression))
                return value;

            return point.PositiveExpression.ToUpperInvariant() switch
            {
                "HEX2DEC" => NumberBaseConverter.HexToDecimal(value), //工具静态类，十六进制转十进制
                "DEC2HEX" => NumberBaseConverter.DecimalToHex(value, false), //工具静态类，十进制转十六进制
                _ => EvaluateExpression(point.PositiveExpression, value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "点位值转换失败:  {Expression}, 原值: {Value}", point.PositiveExpression, value);
            return value;
        }
    }

    private static object? EvaluateExpression(string expression, object? value)
    {
        if (value == null || !NumericTypeChecker.IsNumeric(value)) //工具静态类，判断值是哪种类型：数值原生类型、JsonElement、可解析的数值字符串
            return value;

        double numericValue = value switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.Number => je.GetDouble(),
            _ => System.Convert.ToDouble(value)
        };

        try
        {
            return SingleVariableExpressionEvaluator.Evaluate(expression, numericValue);
        }
        catch (Exception)
        {
            // 表达式计算失败，返回四舍五入后的原值作为降级处理
            return SingleVariableExpressionEvaluator.RoundToTwoDecimals(numericValue);
        }
    }

    /// <summary>
    /// 【新增】根据写入的值，逆推还原为底层驱动需要的物理值
    /// </summary>
    public object? ConvertInverse(ParameterConfig point, object? writeValue)
    {
        try
        {
            if (string.IsNullOrEmpty(point.PositiveExpression))
                return writeValue;

            // 逆向反推操作
            return point.PositiveExpression.ToUpperInvariant() switch
            {
                "HEX2DEC" => NumberBaseConverter.DecimalToHex(writeValue, false), // 读是16转10，写就是10转16
                "DEC2HEX" => NumberBaseConverter.HexToDecimal(writeValue),        // 反之亦然
                _ => EvaluateInverseExpression(point.PositiveExpression, writeValue)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "点位写入逆向转换失败: {Expression}, 伪下发值: {Value}", point.PositiveExpression, writeValue);
            return writeValue; // 尽力而为，如果失败退回原先的输入值
        }
    }

    /// <summary>
    /// 【新增】解析一元一次方程逆推。例如 expression 为 "x*10"，若 value 为 100，则算出并返回 10。
    /// </summary>
    private static object? EvaluateInverseExpression(string expression, object? value)
    {
        if (value == null || !NumericTypeChecker.IsNumeric(value))
            return value;

        double numericValue = value switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.Number => je.GetDouble(),
            _ => System.Convert.ToDouble(value)
        };

        try
        {
            // 基于代数法两点求斜率的优秀工具类
            return SingleVariableExpressionEvaluator.InverseEvaluate(expression, numericValue);
        }
        catch (Exception)
        {
            // 如果它不是一元一次方程或是非法的，退回原始值
            return numericValue;
        }
    }
}
