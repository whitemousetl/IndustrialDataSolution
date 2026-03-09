namespace IndustrialDataProcessor.Domain.Repositories;

public interface IEquipmentDataStorageRepository
{
    /// <summary>
    /// 保存设备数据到数据库
    /// </summary>
    Task SaveEquipmentDataAsync(string equipmentId, string collectionData, CancellationToken token);
}
