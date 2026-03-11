using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Infrastructure.Persistence.DbEntities;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Persistence.Repositories;

/// <summary>
/// 设备数据存储仓储实现，负责将设备实时数据写入TimescaleDB
/// </summary>
public class EquipmentDataStorageRepository : IEquipmentDataStorageRepository
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<EquipmentDataStorageRepository> _logger;

    public EquipmentDataStorageRepository(ISqlSugarClient db, ILogger<EquipmentDataStorageRepository> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db), "SqlSugar数据库客户端不能为空");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "日志器不能为空");
    }

    /// <summary>
    /// 保存设备数据
    /// </summary>
    public async Task SaveEquipmentDataAsync(string equipmentId, string collectionData, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(equipmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionData);

        try
        {
            var data = new EquipmentData
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
            _logger.LogWarning(ex, "设备 {EquipmentId} 数据存储操作被取消", equipmentId);
        }
        catch (SqlSugarException ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 数据存储失败（数据库异常）", equipmentId);
            throw new InvalidOperationException($"设备 {equipmentId} 数据库存储失败", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备 {EquipmentId} 数据存储失败（未知异常）", equipmentId);
            throw;
        }
    }
}
