using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Workstation;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers;

/// <summary>
/// 协议驱动的抽象基类，提供通用的连接保障、异常捕获和读写流程编排（模板方法模式）
/// </summary>
/// <typeparam name="TConnection">底层通信连接类型（如 ModbusTcpNet, ModbusRtu 等）</typeparam>
public abstract class BaseProtocolDriver<TConnection> : IProtocolDriver where TConnection : class
{
    protected readonly string _protocolName;

    protected BaseProtocolDriver()
    {

        // 自动从类名中提取协议名称，例如 ModbusTcpDriver -> ModbusTcp
        _protocolName = GetType().Name.EndsWith("Driver")
            ? GetType().Name[..^"Driver".Length]
            : GetType().Name;
    }

    #region IProtocolDriver 实现 (流程编排)

    public virtual async Task<PointResult?> ReadAsync(IConnectionHandle handle, ProtocolConfig protocol, string equipmentId, ParameterConfig point, CancellationToken token)
    {
        try
        {
            // 核心：获取通道锁！确保同一串口/TCP通道不会发生并发冲突
            using var releaser = await handle.AcquireLockAsync(token);

            // 读取具体点位（具体读取逻辑由子类实现）
            return await ReadPointCoreAsync(handle, point, token);
        }
        catch (Exception ex)
        {
            // 统一异常包装，方便上层捕获和记录日志
            throw new Exception($"{_protocolName}协议读取点位[{point.Address}]失败: {ex.Message}", ex);
        }
    }

    public virtual async Task<bool> WriteAsync(IConnectionHandle handle, WriteTask writeTask, object value, CancellationToken token)
    {
        // 【核心新增1】拦截并跳过虚拟点，伪装为“写入成功”告知上层（也可看业务需求返回false）
        if (!string.IsNullOrEmpty(writeTask.WritePoint.Address) && writeTask.WritePoint.Address.Contains("VirtualPoint", StringComparison.OrdinalIgnoreCase))
        {
            // 这个操作不走网络
            return false;
        }

        try
        {
            // 核心：获取通道锁！确保同一串口/TCP通道不会发生并发冲突
            using var releaser = await handle.AcquireLockAsync(token);
  
            bool isSuccess = true;

            var success = await WritePointCoreAsync(handle, writeTask.WritePoint, value, token);
            if (!success) isSuccess = false;

            return isSuccess;
        }
        catch (OperationCanceledException)
        {
            throw; // 正常抛出取消异常
        }
        catch (Exception ex)
        {
            throw new Exception($"{_protocolName}协议写入失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 默认不支持整包读取，支持的协议（如某些自定义协议或非标协议）可在子类中重写此方法
    /// </summary>
    public virtual Task<ProtocolResult?> ReadAsync(IConnectionHandle handle, ProtocolConfig protocol, CancellationToken token)
    {
        // 默认不支持整包读取，支持的协议（如某些自定义协议或非标协议）可在子类中重写此方法
        throw new NotImplementedException($"{_protocolName} 协议不支持整包读取 (ProtocolResult)。");
    }

    public string GetProtocolName() => _protocolName;

    #endregion

    #region 抽象方法 - 交由具体协议子类实现核心逻辑

    /// <summary>
    /// 执行单个点位的读取（包含站号设置、调用 HSL 扩展方法等）
    /// </summary>
    protected abstract Task<PointResult> ReadPointCoreAsync(IConnectionHandle handle, ParameterConfig point, CancellationToken token);

    /// <summary>
    /// 执行单个点位的写入（包含站号设置、调用 HSL 扩展方法等）
    /// </summary>
    protected abstract Task<bool> WritePointCoreAsync(IConnectionHandle handle, ParameterConfig point, object value, CancellationToken token);
    #endregion

    #region IDisposable 实现

    public virtual void Dispose()
    {
        // 驱动现在是无状态的，不再持有底层连接，因此不需要释放任何资源
        GC.SuppressFinalize(this);
    }
    #endregion
}