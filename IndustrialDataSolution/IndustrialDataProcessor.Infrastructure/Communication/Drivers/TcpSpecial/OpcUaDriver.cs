using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using Opc.Ua;
using Opc.Ua.Client;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.TcpSpecial;

/// <summary>
/// OPC UA 协议驱动（客户端采集模式）
/// 架构与其他驱动完全对称：
///   连接管理：ConnectionManager 创建 ISession 并包装为 DefaultConnectionHandle
///   地址格式：OPC UA 标准 NodeId 字符串，如 "ns=2;s=Tag.输入.Uint引风机运行电流"
///             支持所有 OPC UA NodeId 格式：
///               数値 Id   : ns=2;i=1001
///               字符串 Id : ns=2;s=MyTag
///               GUID  Id  : ns=2;g=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
///             命名空间索引 ns=0 可省略（默认命名空间）
/// </summary>
public class OpcUaDriver : BaseProtocolDriver<ISession>
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

        if (string.IsNullOrWhiteSpace(point.Address))
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = "OPC UA 节点地址（Address）不能为空";
            return result;
        }

        try
        {
            var session = handle.GetRawConnection<ISession>();

            // 解析 NodeId（支持 ns=2;s=xxx / ns=2;i=xxx / ns=2;g=xxx 等标准格式）
            NodeId nodeId;
            try
            {
                nodeId = NodeId.Parse(point.Address);
            }
            catch (Exception ex)
            {
                result.ReadIsSuccess = false;
                result.ErrorMsg = $"OPC UA 节点地址格式无效 '{point.Address}': {ex.Message}";
                return result;
            }

            // 读取节点值（原生异步，不阻塞线程池）
            DataValue dataValue = await session.ReadValueAsync(nodeId, token);

            if (!StatusCode.IsGood(dataValue.StatusCode))
            {
                result.ReadIsSuccess = false;
                result.ErrorMsg = $"读取节点 '{point.Address}' 返回非 Good 状态: {dataValue.StatusCode}";
                return result;
            }

            result.Value = dataValue.Value?.ToString();
            result.ReadIsSuccess = true;
            return result;
        }
        catch (ServiceResultException ex)
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = $"OPC UA 服务调用失败: {ex.Message} (StatusCode: {ex.StatusCode})";
            return result;
        }
        catch (Exception ex)
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = $"OPC UA 读取异常: {ex.Message}";
            return result;
        }
    }

    protected override async Task<bool> WritePointCoreAsync(
        IConnectionHandle handle,
        ParameterConfig point,
        object value,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(point.Address))
            return false;

        try
        {
            var session = handle.GetRawConnection<ISession>();

            NodeId nodeId;
            try
            {
                nodeId = NodeId.Parse(point.Address);
            }
            catch
            {
                return false;
            }

            var writeValue = new WriteValue
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };

            var writeValues = new WriteValueCollection { writeValue };

            WriteResponse writeResponse = await session.WriteAsync(null, writeValues, token);
            var statusCodes = writeResponse.Results;

            return statusCodes != null && statusCodes.Count > 0 && StatusCode.IsGood(statusCodes[0]);
        }
        catch
        {
            return false;
        }
    }
}
