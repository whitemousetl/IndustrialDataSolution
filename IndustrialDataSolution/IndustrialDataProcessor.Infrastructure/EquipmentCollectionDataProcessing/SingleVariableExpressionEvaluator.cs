using DynamicExpresso;
using DynamicExpresso.Exceptions;

namespace IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;

/// <summary>
/// 单变量数学表达式求值器
/// 支持: x*0.02+1, x/100, x+5 等
/// </summary>
public static class SingleVariableExpressionEvaluator
{

    /// <summary>
    /// 计算单变量表达式，x为变量
    /// </summary>
    public static double Evaluate(string expression, double x)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        try
        {
            var interpreter = new Interpreter(); // 每次新建，线程安全

            var parameters = new Parameter[] { new("x", typeof(double), x) };
            var doubleValue = Convert.ToDouble(interpreter.Eval(expression, parameters));

            // 检查无效的浮点数结果
            if (double.IsInfinity(doubleValue))
                throw new ArgumentException($"表达式 '{expression}' 计算失败 (x={x}): 结果超出有效范围（可能是除以零或数值溢出）", nameof(expression));
            if (double.IsNaN(doubleValue))
                throw new ArgumentException($"表达式 '{expression}' 计算失败 (x={x}): 结果无效", nameof(expression));

            return RoundToTwoDecimals(doubleValue);
        }
        catch (ParseException ex)
        {
            // 解析错误与 x 值无关，可以不加
            throw new ArgumentException($"表达式 '{expression}' 格式无效: {GetFriendlyMessage(ex)}", nameof(expression), ex);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException)
        {
            throw new InvalidOperationException($"表达式 '{expression}' 结果无法转换为数值 (x={x})", ex);
        }
    }

    /// <summary>
    /// 四舍五入到两位小数（使用远离零的舍入方式）
    /// </summary>
    public static double RoundToTwoDecimals(double value)
    {
        // decimal 范围检查，超出范围时回退到 double 四舍五入
        if (value is > (double)decimal.MaxValue or < (double)decimal.MinValue)
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);

        return (double)Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
    }

    private static string GetFriendlyMessage(ParseException ex)
    {
        // 根据常见错误提供友好提示
        if (ex.Message.Contains("Invalid Operation"))
            return "表达式不完整，请检查运算符后是否缺少操作数";
        if (ex.Message.Contains("Unknown identifier"))
            return "包含未知的标识符";
        return ex.Message;
    }

    /// <summary>
    /// 反解一元一次方程:  已知 y = f(x)，求 x
    /// 支持: x*a+b, x/a+b, x+b, x-b
    /// </summary>
    public static double InverseEvaluate(string expression, double y)
    {
        var interpreter = new Interpreter();

        double f0 = EvaluateInternal(interpreter, expression, 0);
        double f1 = EvaluateInternal(interpreter, expression, 1);
        double f2 = EvaluateInternal(interpreter, expression, 2); // 第三点验证

        double a = f1 - f0;
        double b = f0;

        // 验证线性：f(2) 应该等于 2a + b
        double expectedF2 = 2 * a + b;
        if (Math.Abs(f2 - expectedF2) > 1e-9)
            throw new ArgumentException($"表达式 '{expression}' 不是一元一次方程");

        if (Math.Abs(a) < 1e-12)
            throw new ArgumentException($"该表达式 '{expression}' 不是一元一次方程（x 系数为 0）");

        double x = (y - b) / a;
        return RoundToTwoDecimals(x);
    }

    private static double EvaluateInternal(Interpreter interpreter, string expression, double x)
    {
        var parameters = new[] { new Parameter("x", typeof(double), x) };
        var result = Convert.ToDouble(interpreter.Eval(expression, parameters));

        if (double.IsNaN(result) || double.IsInfinity(result))
            throw new InvalidOperationException("表达式计算结果无效");

        return result;
    }
}