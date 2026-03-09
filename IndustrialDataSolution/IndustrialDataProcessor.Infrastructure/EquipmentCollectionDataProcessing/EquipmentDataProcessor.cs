using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using System.Collections.Concurrent;
using System.Text.Json;

namespace IndustrialDataProcessor.Infrastructure.EquipmentCollectionDataProcessing;

public class EquipmentDataProcessor : IEquipmentDataProcessor
{
    private readonly VirtualPointCalculator _virtualPointCalculator;
    private readonly PointExpressionConverter _pointExpressionConverter;
    private readonly JsonSerializerOptions _jsonOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public EquipmentDataProcessor(PointExpressionConverter pointExpressionConverter, VirtualPointCalculator virtualPointCalculator)
    {
        _pointExpressionConverter = pointExpressionConverter;
        _virtualPointCalculator = virtualPointCalculator;
    }

    public ConcurrentDictionary<string, string> Process(ProtocolResult protocolResult, ProtocolConfig protocol, CancellationToken token)
    {
        var equipmentJsonDataMap = new ConcurrentDictionary<string, string>();
        foreach (var equipmentResult in protocolResult.EquipmentResults)
        {
            if (string.IsNullOrEmpty(equipmentResult.EquipmentId) || equipmentResult.PointResults == null || equipmentResult.PointResults.Count == 0) continue; // 如果设备设备结果的设备id为空 或 设备结果的点结果列表为空 或 设备结果的点结果列表数量是0 跳过当前设备结果

            //设备结果转换， 有两个固定点，设备id 和 时间戳
            var forwardEquipmentResult = new ConcurrentDictionary<string, object?>();

            var equipment = protocol.Equipments.FirstOrDefault(d => d.Id == equipmentResult.EquipmentId); // 从协议中找到对应设备

            if (equipment == null) continue; // 如果对应设备为空， 跳过当前设备结果

            // 收集虚拟点
            var virtualPoints = new ConcurrentBag<ParameterConfig>();

            // 1. 处理设备结果，收集正常数据并完成公式转换
            ProcessEquipmentResult(equipmentResult, equipment, virtualPoints, forwardEquipmentResult);
            // 2. 处理虚拟点计算、序列化，并回写最新结果到 equipmentResult 对象中
            SerializeEquipmentData(equipmentResult, forwardEquipmentResult, virtualPoints, equipmentJsonDataMap);
        }

        // 【核心改动 2】在此时完成所有的虚拟点、公式转换之后，一站式拉并计算所有的状态
        CalculateFinalAggregateStatus(protocolResult);

        return equipmentJsonDataMap;
    }

    private void ProcessEquipmentResult(EquipmentResult equipmentResult, EquipmentConfig equipment, ConcurrentBag<ParameterConfig> virtualPoints, ConcurrentDictionary<string, object?> forwardEquipmentResult)
    {
        foreach (var pointResult in equipmentResult.PointResults)
        {
            if (!IsValidPointResult(pointResult)) continue; // 点结果或点结果标签为空， 跳过当前设备结果
            var point = equipment?.Parameters?.FirstOrDefault(p => p.Label == pointResult.Label); //从设备中找到与设备结果标签一样的点，目的找到配置中该点的转换条件或虚拟点
            if (point == null) continue;

            if (point.Address.Contains("VirtualPoint"))
            {
                virtualPoints.Add(point);
                continue; // 虚拟点先不处理，等下面一起计算
            }

            try
            {
                var finalValue = _pointExpressionConverter.Convert(point, pointResult.Value);
                if (!string.IsNullOrEmpty(pointResult?.Label))
                {
                    forwardEquipmentResult[pointResult.Label] = finalValue;
                    pointResult.Value = finalValue;
                }
            }
            catch (Exception ex)
            {
                pointResult.ReadIsSuccess = false;
                pointResult.ErrorMsg = $"公式转换异常: {ex.Message}";
            }
        }
    }

    private static bool IsValidPointResult(PointResult pointResult) => pointResult != null && !string.IsNullOrWhiteSpace(pointResult.Label);

    private void SerializeEquipmentData(EquipmentResult equipmentResult, ConcurrentDictionary<string, object?> forwardEquipmentResult, ConcurrentBag<ParameterConfig> virtualPoints, ConcurrentDictionary<string, string> dataEquipmentId)
    {
        // 1. 处理虚拟点 (这个方法内部会把算出的值添加到 forwardEquipmentResult 字典中)
        _virtualPointCalculator.Calculate(virtualPoints, forwardEquipmentResult);

        foreach (var vPoint in virtualPoints)
        {
            if (forwardEquipmentResult.TryGetValue(vPoint.Label, out var calculatedVal))
            {
                var existingPoint = equipmentResult.PointResults.FirstOrDefault(p => p.Label == vPoint.Label);

                if (existingPoint != null)
                {
                    existingPoint.Value = calculatedVal;
                    bool isCalculateSuccess = calculatedVal != null;

                    // 收敛之前的混乱逻辑：直接决定这个虚拟点是成功还是失败
                    if (!isCalculateSuccess)
                    {
                        existingPoint.ReadIsSuccess = false;
                        existingPoint.ErrorMsg = "虚拟点计算失败或未能得出有效值";
                    }
                }
            }
        }

        // 2. 将最终包含了所有转换+计算点位的字典序列化存入将要入库的 map 中
        var data = JsonSerializer.Serialize(forwardEquipmentResult, _jsonOptions);
        dataEquipmentId[equipmentResult.EquipmentId] = data;
    }

    /// <summary>
    /// 当所有处理环节跑完后，进行最终的统计核算
    /// </summary>
    private void CalculateFinalAggregateStatus(ProtocolResult protocolResult)
    {
        // 如果在底层（AppService）已经被判定为全局连不上的致命失败，就不要去重新覆盖状态了
        if (!protocolResult.ReadIsSuccess && protocolResult.EquipmentResults.Count == 0) return;

        protocolResult.SuccessEquipments = 0;
        protocolResult.FailedEquipments = 0;
        protocolResult.TotalPoints = 0;
        protocolResult.SuccessPoints = 0;
        protocolResult.FailedPoints = 0;

        foreach (var eq in protocolResult.EquipmentResults)
        {
            // 此时所有点位的 ReadIsSuccess 才是最终真理
            eq.SuccessPoints = eq.PointResults.Count(p => p.ReadIsSuccess);
            eq.FailedPoints = eq.PointResults.Count(p => !p.ReadIsSuccess);

            bool isFullyProcessed = (eq.SuccessPoints + eq.FailedPoints) == eq.TotalPoints;

            // 设备的成功与否，完全剥离 AppService，在这里做最终宣判
            eq.ReadIsSuccess = eq.TotalPoints > 0 && isFullyProcessed && eq.FailedPoints == 0;

            if (!eq.ReadIsSuccess && string.IsNullOrEmpty(eq.ErrorMsg))
            {
                eq.ErrorMsg = isFullyProcessed ? $"设备下存在 {eq.FailedPoints} 个异常点位" : $"设备采集被中断，完成进度: {(eq.SuccessPoints + eq.FailedPoints)}/{eq.TotalPoints}";
            }

            // 累加协议级状态
            if (eq.ReadIsSuccess) protocolResult.SuccessEquipments++;
            else protocolResult.FailedEquipments++;

            protocolResult.TotalPoints += eq.TotalPoints;
            protocolResult.SuccessPoints += eq.SuccessPoints;
            protocolResult.FailedPoints += eq.FailedPoints;
        }

        // 协议级状态最后落锤
        protocolResult.ReadIsSuccess = protocolResult.FailedEquipments == 0;
    }
}
