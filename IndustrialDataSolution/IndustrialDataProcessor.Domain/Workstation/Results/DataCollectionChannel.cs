using IndustrialDataProcessor.Domain.Workstation.Configs;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace IndustrialDataProcessor.Domain.Workstation.Results;

/// <summary>
/// 一个简单的单例数据通道，用于在组件间传递采集结果
/// </summary>
public class DataCollectionChannel
{
    // 建立三个独立的通道
    private readonly Channel<(ProtocolResult Result, ProtocolConfig Config, ConcurrentDictionary<string, string> JSONMap)> _opcUaChannel;
    private readonly Channel<(ProtocolResult Result, ProtocolConfig Config, ConcurrentDictionary<string, string> JSONMap)> _dbChannel;
    private readonly Channel<(ProtocolResult Result, ProtocolConfig Config, ConcurrentDictionary<string, string> JSONMap)> _heartbeatChannel;

    public DataCollectionChannel()
    {
        _opcUaChannel = Channel.CreateUnbounded<(ProtocolResult Result, ProtocolConfig Config, ConcurrentDictionary<string, string> JSONMap)>();
        _dbChannel = Channel.CreateUnbounded<(ProtocolResult Result, ProtocolConfig Config, ConcurrentDictionary<string, string> JSONMap)>();
        // 心跳通道：有界队列，容量100，超出时丢弃最旧的，避免积压
        _heartbeatChannel = Channel.CreateBounded<(ProtocolResult Result, ProtocolConfig Config, ConcurrentDictionary<string, string> JSONMap)>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest });
    }

    // 暴露独立的 Reader 给不同的后台服务订阅
    public ChannelReader<(ProtocolResult Result, ProtocolConfig Config, ConcurrentDictionary<string, string> JSONMap)> OpcUaChannel => _opcUaChannel.Reader;
    public ChannelReader<(ProtocolResult Result, ProtocolConfig Config, ConcurrentDictionary<string, string> JSONMap)> DbChannel => _dbChannel.Reader;
    /// <summary>心跳上报通道（有界，DropOldest）</summary>
    public ChannelReader<(ProtocolResult Result, ProtocolConfig Config, ConcurrentDictionary<string, string> JSONMap)> HeartbeatChannel => _heartbeatChannel.Reader;

    /// <summary>
    /// 发布数据 (Fan-out 扇出给所有通道)
    /// </summary>
    public async ValueTask PublishAsync(ProtocolResult result, ProtocolConfig protocol, ConcurrentDictionary<string, string> jsonMap, CancellationToken token = default)
    {
        var opcTask = _opcUaChannel.Writer.WriteAsync((result, protocol, jsonMap), token).AsTask();
        var dbTask = _dbChannel.Writer.WriteAsync((result, protocol, jsonMap), token).AsTask();
        // 心跳通道：非阻塞写入（有界通道超容时自动丢弃旧数据，不会阻塞）
        _heartbeatChannel.Writer.TryWrite((result, protocol, jsonMap));

        await Task.WhenAll(opcTask, dbTask);
    }
}
