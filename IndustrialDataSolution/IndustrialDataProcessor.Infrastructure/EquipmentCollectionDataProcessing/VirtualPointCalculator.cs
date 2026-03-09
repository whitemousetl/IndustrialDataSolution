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
        var (variables, normalizedExpression) = VariablePlaceholderParser.Parse(expression);

        var interpreter = new Interpreter();
        foreach (var varName in variables)
        {
            var value = equipmentData.TryGetValue(varName, out var val) ? val ?? 0 : 0;
            interpreter.SetVariable(varName, value);
        }

        return interpreter.Eval(normalizedExpression);
    }
}
