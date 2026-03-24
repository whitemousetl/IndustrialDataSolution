using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;
using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.Sql;

/// <summary>
/// 数据库协议驱动（支持 MySQL）
/// 设计思路与 ApiDriver 完全对称：
///   ApiDriver   : 一次 HTTP 请求 → JSON 响应 → Address(JSON路径) 提取値
///   DatabaseDriver: 一次 SQL 查询 → 结果首行 → Address(列名) 提取値
/// 缓存属于 DatabaseConnectionHandle，不同协议配置完全隔离
/// </summary>
public class MySQLDriver : BaseProtocolDriver<ISqlSugarClient>
{
    protected override async Task<PointResult> ReadPointCoreAsync(
        IConnectionHandle handle,
        ParameterConfig point,
        CancellationToken token)
    {
        var result = new PointResult
        {
            Address = point.Address,
            Label = point.Label,
            DataType = point.DataType
        };

        if (handle is not DatabaseConnectionHandle dbHandle)
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = "连接句柄类型不正确，期望 DatabaseConnectionHandle";
            return result;
        }

        if (string.IsNullOrWhiteSpace(point.Address))
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = "参数地址（列名）不能为空";
            return result;
        }

        if (string.IsNullOrWhiteSpace(dbHandle.QuerySqlString))
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = "协议未配置 QuerySqlString";
            return result;
        }

        try
        {
            // 从连接句柄获取行缓存（同一采集周期内只查一次库）
            var row = await dbHandle.GetOrExecuteQueryAsync(dbHandle.QuerySqlString);

            if (row == null)
            {
                result.ReadIsSuccess = false;
                result.ErrorMsg = $"SQL 未返回任何数据行: {dbHandle.QuerySqlString}";
                return result;
            }

            // 按 Address（列名）提取値，大小写不敏感
            var matchedKey = row.Keys.FirstOrDefault(k =>
                k.Equals(point.Address, StringComparison.OrdinalIgnoreCase));

            if (matchedKey == null)
            {
                result.ReadIsSuccess = false;
                result.ErrorMsg = $"查询结果中不存在列 '{point.Address}'，可用列: {string.Join(", ", row.Keys)}";
                return result;
            }

            result.Value = row[matchedKey]?.ToString();
            result.ReadIsSuccess = true;
            return result;
        }
        catch (Exception ex)
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = $"数据库查询失败: {ex.Message}";
            return result;
        }
    }

    protected override Task<bool> WritePointCoreAsync(
        IConnectionHandle handle,
        ParameterConfig point,
        object value,
        CancellationToken token)
    {
        return Task.FromResult(false);
    }
}
