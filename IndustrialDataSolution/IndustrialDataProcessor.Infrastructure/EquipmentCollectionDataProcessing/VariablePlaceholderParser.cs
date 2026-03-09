namespace IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;

/// <summary>
/// 变量占位符解析器
/// 处理格式: {varName} → varName
/// </summary>
public static class VariablePlaceholderParser
{
    /// <summary>
    /// 从表达式中提取所有变量名
    /// 示例: "{Temperature} + {Humidity}" → ["Temperature", "Humidity"]
    /// </summary>
    private static List<string> ExtractVariableNames(string expression)
    {
        var variables = new List<string>();
        int index = 0;

        while ((index = expression.IndexOf('{', index)) != -1)
        {
            int endIndex = expression.IndexOf('}', index + 1);
            if (endIndex == -1)
                break;

            var variableName = expression.Substring(index + 1, endIndex - index - 1);

            if (!string.IsNullOrWhiteSpace(variableName) && !variables.Contains(variableName))
                variables.Add(variableName);

            index = endIndex + 1;
        }

        return variables;
    }

    /// <summary>
    /// 将表达式中的占位符替换为变量名
    /// 示例: "{Temperature} + {Humidity}" → "Temperature + Humidity"
    /// </summary>
    private static string ReplacePlaceholders(string expression, IEnumerable<string> variableNames)
    {
        string result = expression;

        foreach (var variableName in variableNames)
        {
            result = result.Replace($"{{{variableName}}}", variableName);
        }

        return result;
    }

    /// <summary>
    /// 一次性提取并替换（性能优化版）
    /// </summary>
    public static (List<string> Variables, string NormalizedExpression) Parse(string expression)
    {
        var variables = ExtractVariableNames(expression);
        var normalized = ReplacePlaceholders(expression, variables);
        return (variables, normalized);
    }
}
