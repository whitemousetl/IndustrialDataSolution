using IndustrialDataProcessor.Domain.Workstation.Configs;
using Opc.Ua;
using Opc.Ua.Server;

namespace IndustrialDataProcessor.Infrastructure.OpcUa;

/// <summary>
/// 工作站 OPC UA 服务器封装
/// 负责管理底层的网络通信、证书安全、和自定义节点管理器
/// </summary>
public class WorkstationOpcServer(WorkstationConfig config) : StandardServer
{
    private readonly WorkstationConfig _config = config ?? throw new ArgumentNullException(nameof(config));

    // 暴露 NodeManager 供外部（例如你的应用层事件处理器）调用，以推送最新采集的数据
    public NodeManager CustomNodeManager { get; private set; } = default!;

    /// <summary>
    /// 重写父类方法，这是向服务器注册自定义地址空间的唯一入口
    /// </summary>
    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        // 1. 实例化我们的自定义节点管理器，把环境配置给它
        CustomNodeManager = new NodeManager(server, configuration, _config);

        // 2. 将它放入列表。你甚至可以有多个不同用途的 NodeManager
        var nodeManagers = new List<INodeManager>
        {
            CustomNodeManager
        };

        // 3. 交给 MasterNodeManager 进行统一调度（管理各种节点的路由路由查找）
        return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
    }
}
