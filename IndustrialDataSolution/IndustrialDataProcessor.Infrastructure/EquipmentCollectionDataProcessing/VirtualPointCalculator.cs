using DynamicExpresso;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using Microsoft.Extensions.Logging;

namespace IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;

public class VirtualPointCalculator
{
    private readonly ILogger<VirtualPointCalculator> _logger;

    public VirtualPointCalculator(ILogger<VirtualPointCalculator> logger)
    {
        _logger = logger;
    }

    public void Calculate(IEnumerable<ParameterConfig> virtualPoints, IDictionary<string, object?> equipmentData)
    {
        foreach (var point in virtualPoints)
        {
            if (string.IsNullOrWhiteSpace(point.PositiveExpression))
                continue;

            try
            {
                var result = EvaluateExpression(point.PositiveExpression, equipmentData);
                equipmentData[point.Label] = result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "虚拟点计算失败: {Label}, 表达式: {Expression}", point.Label, point.PositiveExpression);
                equipmentData[point.Label] = null;
            }
        }
    }

    private static object? EvaluateExpression(string expression, IDictionary<string, object?> equipmentData)
    {
        // 布尔转整数专用表达式：BOOL2INT:{VarName}
        // 将指定变量的布尔值直接转为 0/1，跳过 DynamicExpresso 表达式引擎，转换后直接发布
        if (expression.StartsWith("BOOL2INT:", StringComparison.OrdinalIgnoreCase))
        {
            var varName = expression["BOOL2INT:".Length..].Trim().Trim('{', '}');
            var rawValue = equipmentData.TryGetValue(varName, out var boolVal) ? boolVal : null;
            return ConvertBoolToInt(rawValue);
        }

        var (variables, normalizedExpression) = VariablePlaceholderParser.Parse(expression);

        var interpreter = new Interpreter();
        foreach (var varName in variables)
        {
            var rawValue = equipmentData.TryGetValue(varName, out var val) ? val ?? 0 : 0;
            // 布尔类型自动转为 int(0/1)，避免 DynamicExpresso 对 bool 做数学运算时抛出异常
            var value = rawValue is bool boolVal ? (boolVal ? 1 : 0) : rawValue;
            interpreter.SetVariable(varName, value);
        }

        return interpreter.Eval(normalizedExpression);
    }

    /// <summary>
    /// 将各种形式的布尔值转换为整数（1 或 0）
    /// 支持：C# bool、字符串 "True"/"False"、"是"/"否"、"Yes"/"No"、"1"/"0"
    /// </summary>
    private static int ConvertBoolToInt(object? value)
    {
        if (value == null) return 0;
        if (value is bool b) return b ? 1 : 0;

        string str = value.ToString()?.Trim() ?? string.Empty;
        return str.ToUpperInvariant() switch
        {
            "TRUE" or "是" or "YES" or "1" => 1,
            "FALSE" or "否" or "NO" or "0" => 0,
            _ => double.TryParse(str, out var d) ? (d != 0 ? 1 : 0) : 0
        };
    }
}
