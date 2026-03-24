using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;
using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.Sql;

/// <summary>
/// 数据库协议驱动基类（MySQL 和 SQLite 共享完全相同的读取逻辑）
///
/// Address 地址格式（两种，向后兼容）：
///   ┌─────────────────────────────────────────────────────────────────┐
///   │ 单行模式 │ "ColumnName"                                        │
///   │          │ 适用于 SQL 只返回一行数据的场景                      │
///   │          │ 示例：GrandTotalProducepie                           │
///   ├─────────────────────────────────────────────────────────────────┤
///   │ 多行模式 │ "RowKeyValue|ColumnName"                            │
///   │          │ 适用于 SQL 返回多行（多台设备）的场景                │
///   │          │ RowKeyValue：与结果集第一列匹配的行标识值            │
///   │          │ ColumnName ：要提取的目标列名                        │
///   │          │ 示例：1D54上砖机|GrandTotalProducepie               │
///   └─────────────────────────────────────────────────────────────────┘
///
/// 设计原则：
///   - QuerySqlString（配置在 DatabaseInterfaceConfig）负责"取什么数据"
///   - Address 负责"从哪行、哪列提取值"
///   - 两者结合，完全对称 ApiDriver 的 AccessApiString + Address(JSON路径) 模式
/// </summary>
public abstract class BaseDatabaseDriver : BaseProtocolDriver<ISqlSugarClient>
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

        // ── 前置验证 ──────────────────────────────────────────────────────
        if (handle is not DatabaseConnectionHandle dbHandle)
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = "连接句柄类型不正确，期望 DatabaseConnectionHandle";
            return result;
        }

        if (string.IsNullOrWhiteSpace(point.Address))
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = "参数地址（Address）不能为空";
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
            // ── 查询缓存（同一采集周期内同一 SQL 只查一次库）────────────
            var rows = await dbHandle.GetAllRowsAsync(dbHandle.QuerySqlString);
            if (rows == null || rows.Count == 0)
            {
                result.ReadIsSuccess = false;
                result.ErrorMsg = $"SQL 未返回任何数据行";
                return result;
            }

            // ── 解析 Address：判断单行模式 vs 多行模式────────────────────
            string? rowKeyValue = null;
            string columnName;

            var pipeIndex = point.Address.IndexOf('|');
            if (pipeIndex >= 0)
            {
                // 多行模式：「行标识值|列名」
                rowKeyValue = point.Address[..pipeIndex].Trim();
                columnName  = point.Address[(pipeIndex + 1)..].Trim();
            }
            else
            {
                // 单行模式：「列名」
                columnName = point.Address.Trim();
            }

            // ── 定位目标行────────────────────────────────────────────────
            Dictionary<string, object?> targetRow;
            if (rowKeyValue == null)
            {
                // 单行模式：直接使用首行
                targetRow = rows[0];
            }
            else
            {
                // 多行模式：以结果集第一列为行标识列，按值匹配目标行
                var keyColumn = rows[0].Keys.First();
                var matched = rows.FirstOrDefault(r =>
                    r.TryGetValue(keyColumn, out var v) &&
                    v?.ToString()?.Equals(rowKeyValue, StringComparison.OrdinalIgnoreCase) == true);

                if (matched == null)
                {
                    result.ReadIsSuccess = false;
                    result.ErrorMsg =
                        $"在首列 '{keyColumn}' 中找不到值为 '{rowKeyValue}' 的行" +
                        $"（共 {rows.Count} 行，首列可用值：{string.Join(", ", rows.Select(r => r.GetValueOrDefault(keyColumn)?.ToString()))}）";
                    return result;
                }
                targetRow = matched;
            }

            // ── 按列名提取值（大小写不敏感）──────────────────────────────
            var matchedKey = targetRow.Keys.FirstOrDefault(k =>
                k.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (matchedKey == null)
            {
                result.ReadIsSuccess = false;
                result.ErrorMsg =
                    $"查询结果中不存在列 '{columnName}'，可用列: {string.Join(", ", targetRow.Keys)}";
                return result;
            }

            result.Value = targetRow[matchedKey]?.ToString();
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
        => Task.FromResult(false);
}
