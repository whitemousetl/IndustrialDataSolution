using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.DbEntities;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.Repositories;

/// <summary>
/// 设备数据存储服务，负责将设备实时数据写入TimescaleDB
/// </summary>
public class EquipmentDataStorageRepository : IEquipmentDataStorageRepository
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<EquipmentDataStorageRepository> _logger;

    /// <summary>
    /// 初始化设备数据存储服务
    /// </summary>
    /// <param name="db">SqlSugar数据库客户端（必选）</param>
    /// <param name="logger">日志器（必选）</param>
    /// <exception cref="ArgumentNullException">当db或logger为null时抛出</exception>
    public EquipmentDataStorageRepository(ISqlSugarClient db, ILogger<EquipmentDataStorageRepository> logger)
    {
        // 1. 构造函数核心依赖校验：提前暴露错误，避免后续NRE
        _db = db ?? throw new ArgumentNullException(nameof(db), "SqlSugar数据库客户端不能为空");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "日志器不能为空");
    }

    /// <summary>
    /// 保存设备数据（字典入参）
    /// </summary>
    /// <param name="equipmentId">设备ID（非空/非空白）</param>
    /// <param name="validDataDict">设备有效数据字典（非null/非空）</param>
    /// <param name="token">取消令牌</param>
    /// <exception cref="ArgumentException">equipmentId为空/空白时抛出</exception>
    /// <exception cref="ArgumentNullException">validDataDict为null时抛出</exception>
    /// <exception cref="InvalidOperationException">validDataDict为空字典时抛出</exception>
    public async Task SaveEquipmentDataAsync(string equipmentId, string collectionData, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(equipmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionData);

        try
        {
            var data = new EquipmentData()
            {
                Time = DateTimeOffset.Now,
                LocalTime = DateTime.Now,
                EquipmentId = equipmentId,
                Values = collectionData,
            };

            await _db.Insertable(data).ExecuteCommandAsync(token);
        }
        catch (OperationCanceledException ex)
        {
            // 取消操作：记录警告，不向上抛出（取消是预期行为）
            _logger.LogWarning(ex, "设备 {EquipmentId} 数据存储操作被取消", equipmentId);
        }
        catch (SqlSugarException ex)
        {
            // 数据库异常：记录错误并重新抛出
            _logger.LogError(ex, "设备 {EquipmentId} 数据存储失败（数据库异常）", equipmentId);
            throw new InvalidOperationException($"设备 {equipmentId} 数据库存储失败", ex);
        }
        catch (Exception ex)
        {
            // 其他异常：记录错误并重新抛出
            _logger.LogError(ex, "设备 {EquipmentId} 数据存储失败（未知异常）", equipmentId);
            throw;
        }
    }
}
