using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Domain.Workstation.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IndustrialDataProcessor.Infrastructure.BackgroundServices;

public class EquipmentDataHostingService(DataCollectionChannel channel, IEquipmentDataStorageRepository equipmentDataStorageRepository, ILogger<EquipmentDataHostingService> logger) : BackgroundService
{
    private readonly DataCollectionChannel _channel = channel;
    IEquipmentDataStorageRepository _repo = equipmentDataStorageRepository;
    private readonly ILogger<EquipmentDataHostingService> _logger = logger;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // 直接在这里执行死循环等待，一旦 stoppingToken 触发（程序关闭），就会抛出取消异常并退出
            await foreach (var (result, _, JsonMap) in _channel.DbChannel.ReadAllAsync(stoppingToken))
            {
                try
                {
                    if (result.AllEquipmentsFailed()) continue;
                    foreach (var res in JsonMap)
                    {
                        var eqResult = result.EquipmentResults.FirstOrDefault(e => e.EquipmentId == res.Key);
                        if (eqResult == null || eqResult.AllPointsFailed()) continue;
                        await _repo.SaveEquipmentDataAsync(res.Key, res.Value, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    // 这里记得加日志记录异常，否则单条数据解析报错被吞掉，不易排查
                    _logger.LogError(ex, "持久化设备数据时发生异常");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 程序正常退出时引发的取消，正常捕获忽略即可
        }
    }
}
