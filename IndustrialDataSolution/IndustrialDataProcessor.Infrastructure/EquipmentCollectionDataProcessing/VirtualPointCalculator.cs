using DynamicExpresso;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using Microsoft.Extensions.Logging;

namespace IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;

/// <summary>
/// 虚拟点计算结果，包含计算值和源点依赖状态
/// </summary>
public record VirtualPointCalcResult(
    object? Value,
    bool IsSuccess,
    string? ErrorMsg,
    IReadOnlyList<string> FailedSourcePoints
);

public class VirtualPointCalculator
{
    private readonly ILogger<VirtualPointCalculator> _logger;

    public VirtualPointCalculator(ILogger<VirtualPointCalculator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 计算虚拟点（带故障传播机制）
    /// </summary>
    /// <param name="virtualPoints">虚拟点配置列表</param>
    /// <param name="equipmentData">设备数据字典（用于计算）</param>
    /// <param name="pointResults">点位采集结果列表（用于检查源点状态）</param>
    /// <returns>虚拟点计算结果字典</returns>
    public Dictionary<string, VirtualPointCalcResult> Calculate(
        IEnumerable<ParameterConfig> virtualPoints,
        IDictionary<string, object?> equipmentData,
        IReadOnlyList<PointResult> pointResults)
    {
        var results = new Dictionary<string, VirtualPointCalcResult>();

        foreach (var point in virtualPoints)
        {
            if (string.IsNullOrWhiteSpace(point.PositiveExpression))
                continue;

            var calcResult = EvaluateWithDependencyCheck(point, equipmentData, pointResults);
            results[point.Label] = calcResult;

            // 只有计算成功时才更新 equipmentData（供后续虚拟点依赖使用）
            if (calcResult.IsSuccess)
            {
                equipmentData[point.Label] = calcResult.Value;
            }
        }

        return results;
    }

    /// <summary>
    /// 带依赖检查的表达式计算（核心故障传播逻辑）
    /// </summary>
    private VirtualPointCalcResult EvaluateWithDependencyCheck(
        ParameterConfig point,
        IDictionary<string, object?> equipmentData,
        IReadOnlyList<PointResult> pointResults)
    {
        try
        {
            string expression = point.PositiveExpression!;

            // 1. 解析表达式中引用的源变量名
            var sourceVariables = ParseSourceVariables(expression);

            // 2. 检查所有源点的采集状态（故障传播核心）
            var failedSources = new List<string>();
            foreach (var varName in sourceVariables)
            {
                var sourcePoint = pointResults.FirstOrDefault(p => p.Label == varName);
                if (sourcePoint != null && !sourcePoint.ReadIsSuccess)
                {
                    failedSources.Add(varName);
                }
            }

            // 3. 如果有任何源点采集失败，虚拟点继承失败状态（不计算）
            if (failedSources.Count > 0)
            {
                string errorMsg = $"源点采集失败: {string.Join(", ", failedSources)}";
                _logger.LogWarning("虚拟点 {Label} 因源点失败而跳过计算: {FailedSources}", point.Label, errorMsg);
                return new VirtualPointCalcResult(null, false, errorMsg, failedSources);
            }

            // 4. 检查源变量是否存在有效值
            var missingVariables = new List<string>();
            foreach (var varName in sourceVariables)
            {
                if (!equipmentData.TryGetValue(varName, out var val) || val == null)
                {
                    missingVariables.Add(varName);
                }
            }

            if (missingVariables.Count > 0)
            {
                string errorMsg = $"源变量值缺失: {string.Join(", ", missingVariables)}";
                _logger.LogWarning("虚拟点 {Label} 源变量值缺失: {Missing}", point.Label, errorMsg);
                return new VirtualPointCalcResult(null, false, errorMsg, missingVariables);
            }

            // 5. 所有源点正常，执行计算
            var result = EvaluateExpression(expression, equipmentData);
            return new VirtualPointCalcResult(result, true, null, []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "虚拟点计算异常: {Label}, 表达式: {Expression}", point.Label, point.PositiveExpression);
            return new VirtualPointCalcResult(null, false, $"计算异常: {ex.Message}", []);
        }
    }

    /// <summary>
    /// 解析表达式中引用的源变量名
    /// </summary>
    private static IReadOnlyList<string> ParseSourceVariables(string expression)
    {
        // 处理 BOOL2INT:{VarName} 格式
        if (expression.StartsWith("BOOL2INT:", StringComparison.OrdinalIgnoreCase))
        {
            var varName = expression["BOOL2INT:".Length..].Trim().Trim('{', '}');
            return [varName];
        }

        // 使用通用占位符解析器提取所有变量
        var (variables, _) = VariablePlaceholderParser.Parse(expression);
        return variables;
    }

    private static object? EvaluateExpression(string expression, IDictionary<string, object?> equipmentData)
    {
        // 布尔转整数专用表达式：BOOL2INT:{VarName}
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

        var result = interpreter.Eval(normalizedExpression);
        // 对浮点型计算结果应用两位小数精度（整型、布尔型等结果不受影响）
        return RoundFloatingPointResult(result);
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

    /// <summary>
    /// 对浮点型计算结果应用两位小数精度，整型、布尔型等不受影响
    /// </summary>
    private static object? RoundFloatingPointResult(object? result)
    {
        return result switch
        {
            double d => SingleVariableExpressionEvaluator.RoundToTwoDecimals(d),
            float f  => SingleVariableExpressionEvaluator.RoundToTwoDecimals(f),
            decimal m => (double)Math.Round(m, 2, MidpointRounding.AwayFromZero),
            _        => result  // int、bool、string 等原封不动
        };
    }
}