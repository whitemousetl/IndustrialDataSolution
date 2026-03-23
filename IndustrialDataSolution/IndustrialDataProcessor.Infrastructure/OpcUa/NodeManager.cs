using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using Opc.Ua;
using Opc.Ua.Server;
using System.Collections.Concurrent;

namespace IndustrialDataProcessor.Infrastructure.OpcUa;

public class NodeManager : CustomNodeManager2
{
    // <summary>
    private readonly WorkstationConfig _config;

    // 用字典把生成好的"叶子节点"（变量）缓存起来，以便后续快速根据标签/ID找到它们并更新值
    // 同时存储节点声明的 DataType，用于类型转换
    private readonly Dictionary<string, (BaseDataVariableState Node, DataType? DeclaredDataType)> _pointNodes = new();

    // 定义一个委托/事件，用于通知外层有客户端要求下发写入操作了
    public event Func<ProtocolConfig, EquipmentConfig, ParameterConfig, object, Task<(bool isSuccess, string errorMsg)>>? OnOpcClientWriteRequestedAsync;

    // 【新增】反向映射字典：通过节点的 NodeId (字符串形式), 找到对应的路由图：(协议, 设备, 参数点位)
    private readonly ConcurrentDictionary<string, (ProtocolConfig Protocol, EquipmentConfig Equipment, ParameterConfig Parameter)> _nodeRoutingMap = [];

    /// <summary>
    /// 标准构造函数：传入服务器核心、应用程序配置
    /// namespaceUris: 这个节点管理器负责的命名空间（如 "http://yourdomain.com/SuperUAServer"）
    /// </summary>
    public NodeManager(IServerInternal server, ApplicationConfiguration configuration, WorkstationConfig config) : base(server, configuration, "http://yourdomain.com/SuperUAServer")
	{
        _config = config;
        // 设置节点 ID 生成工厂为当前实例
        // 当后续创建 Node 时如果不显式指定 NodeId，将调用本类的 New 方法生成
        SystemContext.NodeIdFactory = this;
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        // 1. 创建根节点: 工作站文件夹 (以工作站名称或ID命名)
        var workstationFolder = CreateFolder(ObjectIds.ObjectsFolder, _config.Id ?? "Workstation", externalReferences);

        // 2. 遍历配置，找到所有启动的设备
        foreach(var protocol in _config.Protocols)
        {
            foreach(var equipment in protocol.Equipments.Where(e => e.IsCollect))
            {
                if (equipment.Parameters == null || equipment.Parameters.Count == 0) continue;

                // 3. 为每个设备在其下创建一层文件夹 (使用 EquipmentId 作为名称)
                var equipmentFolder = CreateFolder(workstationFolder.NodeId, equipment.Id, externalReferences);

                // 4. 为设备下的每个监控点创建变量节点
                //foreach(var point in equipment.Parameters.Where(p => p.IsMonitor))
                foreach(var point in equipment.Parameters)
                {
                    // 数据类型映射 
                    NodeId opcDataType = MapToOpcDataType(point.DataType);

                    // 5. 在设备文件夹下创建叶子变量节点 (用 Label 命名)
                    var pointVariable = CreateVariable(equipmentFolder, point.Label, opcDataType, AccessLevels.CurrentReadOrWrite);

                    // 6. (关键) 把这个节点按一个唯一键存入字典，例如 "EquipmentId_Label"，以便在更新数据时能瞬间找到它
                    // 同时存储配置的 DataType，用于后续值更新时的类型转换
                    string cacheKey = $"{equipment.Id}_{point.Label}";
                    _pointNodes[cacheKey] = (pointVariable, point.DataType);

                    //【核心新增】将这个变量的 NodeId 作为键，把协议、设备、点位的配置打包存起来
                    // 注意：这里的 pointVariable.NodeId.Identifier 就是你在 CreateVariable 传的 name (即 point.Label)
                    // 但为了保证全局唯一，建议把变量的 NodeId 改成带设备前缀的，或者直接用 cacheKey 作为 NodeId 的 Identifier。
                    // 假设你的 OpcUa 节点 NodeId 其实就是上面的 cacheKey，我们这样绑定：
                    _nodeRoutingMap[pointVariable.NodeId.Identifier.ToString()!] = (protocol, equipment, point);

                    // 设置一下初始值（根据数据类型给个默认0或空字符串）
                    // 设置初始值，防止客户端连接时没有拿到值报错
                    object defaultValue = GetDefaultValue(point.DataType);
                    // 【核心修改】这里调用重载方法，将初始状态设为 BadWaitingForInitialData (等待初始数据)
                    UpdateVariableWithStatus(pointVariable, defaultValue, point.DataType, StatusCodes.BadWaitingForInitialData);
                }
            }
        }
    }

    public void UpdateDataFromCollectionResult(ProtocolResult result)
    {
        //if (!result.ReadIsSuccess) return;// 如果整个协议读失败了，可以选择不更新或更新质量戳为Bad

        lock (Lock) // 加锁保护内部节点状态
        {
            // 级别1: 协议级彻底失败（例如：断网了、连接被服务端踢了，根本没拿到 EquipmentResults）
            if (result.AllEquipmentsFailed())
            {
                // 此时直接查找该协议下所有的节点缓存，统一打上通讯失败的质量戳
                MarkProtocolNodesAsBad(result.ProtocolId, StatusCodes.BadCommunicationError);
                return; // 直接返回，不必往下找了
            }

            // 级别2: 协议没问题，遍历其设备结果
            foreach (var eqResult in result.EquipmentResults)
            {
                // 设备级失败（例如：由于一根串口线上挂了3个表，只有这一个表断电了不回数据）
                if (eqResult.AllPointsFailed())
                {
                    MarkEquipmentNodesAsBad(eqResult.EquipmentId, StatusCodes.BadNotConnected);
                    continue; // 跨过当前设备，继续解析下个正常的设备
                }

                // 级别3: 设备在线，细致解析每一个点
                foreach (var ptResult in eqResult.PointResults)
                {
                    string cacheKey = $"{eqResult.EquipmentId}_{ptResult.Label}";

                    if (_pointNodes.TryGetValue(cacheKey, out var nodeInfo))
                    {
                        var (variableNode, declaredDataType) = nodeInfo;
                        
                        if (ptResult.ReadIsSuccess && ptResult.Value != null)
                        {
                            // 单点读取成功: 将值转换为声明的数据类型后再更新
                            UpdateVariableWithStatus(variableNode, ptResult.Value, declaredDataType, StatusCodes.Good);
                        }
                            
                        else
                            // 单点读取失败（比如：仅仅是这个设备的这一个寄存器地址配错/无权限了）
                            // 关键：保留节点原来的值（variableNode.Value），仅将状态明确改为 Bad
                            // 单点读取失败: 只更新状态码，保留旧的值
                            UpdateVariableWithStatus(variableNode, variableNode.Value, declaredDataType, StatusCodes.Bad);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 当整个协议掉线时，将其下所有的点位置为 Bad 状态
    /// </summary>
    private void MarkProtocolNodesAsBad(string protocolId, uint statusCode)
    {
        var protocol = _config.Protocols.FirstOrDefault(p => p.Id == protocolId);
        if (protocol == null) return;

        foreach (var equipment in protocol.Equipments.Where(e => e.IsCollect))
        {
            MarkEquipmentNodesAsBad(equipment.Id, statusCode);
        }
    }

    /// <summary>
    /// 当整个设备掉线时，将其下所有的点位置为 Bad 状态
    /// </summary>
    private void MarkEquipmentNodesAsBad(string equipmentId, uint statusCode)
    {
        var equip = _config.Protocols.SelectMany(p => p.Equipments).FirstOrDefault(e => e.Id == equipmentId);
        if (equip?.Parameters == null) return;

        foreach (var point in equip.Parameters)
        {
            string cacheKey = $"{equipmentId}_{point.Label}";
            if (_pointNodes.TryGetValue(cacheKey, out var nodeInfo))
                // 维持旧的值以便画面参考，更新状态码为设备离线状态
                UpdateVariableWithStatus(nodeInfo.Node, nodeInfo.Node.Value, nodeInfo.DeclaredDataType, statusCode);
        }
    }

    /// <summary>
    /// 更新变量节点的值和质量状态，并在此处统一清洗数据类型以对齐 OPC UA 节点的预设声明
    /// </summary>
    /// <param name="variable">要更新的变量</param>
    /// <param name="value">新值 (可能是表达式算完后的 double 等)</param>
    /// <param name="expectedDataType">参数配置中该点位期望的原始数据类型</param>
    /// <param name="statusCode">OPC UA 状态码，如果不传默认是 Good</param>
    private void UpdateVariableWithStatus(BaseDataVariableState variable, object value, DataType? expectedDataType, uint statusCode = StatusCodes.Good)
    {
        // 使用锁确保线程安全
        lock (Lock)
        {
            // 【关键修复】将值转换为声明的数据类型，防止类型不匹配
            var convertedValue = ConvertToExpectedType(value, expectedDataType);
            
            // 设置转换后的值
            variable.Value = convertedValue;

            // 状态码设为 Good，表示数据有效
            variable.StatusCode = statusCode;
            // 更新时间戳为当前 UTC 时间
            variable.Timestamp = DateTime.UtcNow;

            // 关键：清除变更掩码并触发订阅通知
            variable.ClearChangeMasks(SystemContext, false);
        }
    }

    /// <summary>
    /// 将领域模型的数据类型映射为 OPC UA 标准数据类型 ID
    /// </summary>
    /// <param name="dataType">领域数据类型枚举</param>
    /// <returns>OPC UA 的 NodeId</returns>
    private static NodeId MapToOpcDataType(DataType? dataType)
    {
        if (dataType == null) return DataTypeIds.String; // 默认给个 String，以免崩溃

        return dataType.Value switch
        {
            DataType.Bool => DataTypeIds.Boolean,
            DataType.Short => DataTypeIds.Int16,
            DataType.UShort => DataTypeIds.UInt16,
            DataType.Int => DataTypeIds.Int32,
            DataType.UInt => DataTypeIds.UInt32,
            DataType.Long => DataTypeIds.Int64,
            DataType.ULong => DataTypeIds.UInt64,
            DataType.Float => DataTypeIds.Float,
            DataType.Double => DataTypeIds.Double,
            DataType.String => DataTypeIds.String,
            _ => DataTypeIds.String // 未知类型当字符串处理
        };
    }

    /// <summary>
    /// 根据数据类型获取初始默认值
    /// 解决在采集结果未到达前，OPC UA 节点值为空导致抛出变体(Variant)构建异常的问题
    /// </summary>
    private static object GetDefaultValue(DataType? dataType)
    {
        if (dataType == null) return string.Empty;

        return dataType.Value switch
        {
            DataType.Bool => false,
            DataType.Short => (short)0,
            DataType.UShort => (ushort)0,
            DataType.Int => 0,
            DataType.UInt => 0U,
            DataType.Long => 0L,
            DataType.ULong => 0UL,
            DataType.Float => 0.0f,
            DataType.Double => 0.0d,
            DataType.String => string.Empty,
            _ => string.Empty
        };
    }

    /// <summary>
    /// 重写节点 ID 生成方法
    /// 当创建新节点时，如果节点没有预设 ID，则使用此方法生成
    /// </summary>
    public override NodeId New(ISystemContext context, NodeState node)
        => node.NodeId ?? new NodeId(node.BrowseName.Name, NamespaceIndex);// 如果节点已有 ID 则使用它，否则使用浏览名称作为 ID

    /// <summary>
    /// 创建文件夹节点的辅助方法
    /// </summary>
    /// <param name="parentId">父节点 ID</param>
    /// <param name="name">文件夹名称</param>
    /// <param name="externalRefs">外部引用字典</param>
    /// <returns>创建的文件夹状态对象</returns>
    private FolderState CreateFolder(NodeId parentId, string name, IDictionary<NodeId, IList<IReference>> externalRefs)
    {
        // 创建文件夹状态对象
        // null 表示没有父节点状态对象（因为父节点是标准节点）
        var folder = new FolderState(null)
        {
            // 设置节点 ID，使用命名空间索引确保唯一性
            NodeId = new NodeId(name, NamespaceIndex),
            // 浏览名称，客户端浏览地址空间时显示
            BrowseName = new QualifiedName(name, NamespaceIndex),
            // 显示名称，用于用户界面显示
            DisplayName = new LocalizedText(name),
            // 类型定义，指定这是一个文件夹类型
            TypeDefinitionId = ObjectTypeIds.FolderType
        };

        // 将节点添加到预定义节点集合
        // 这使节点成为地址空间的一部分
        AddPredefinedNode(SystemContext, folder);

        // 处理外部引用
        // 外部引用用于连接到其他节点管理器管理的节点
        if (!externalRefs.TryGetValue(parentId, out var refs))
        {
            // 如果父节点还没有引用列表，创建一个
            externalRefs[parentId] = refs = new List<IReference>();
        }

        // 添加从父节点到此文件夹的 Organizes 引用
        // false 表示这是正向引用（父 -> 子）
        refs.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, folder.NodeId));

        // 添加从此文件夹到父节点的反向引用
        // true 表示这是反向引用（子 -> 父）
        folder.AddReference(ReferenceTypeIds.Organizes, true, parentId);

        return folder;
    }

    /// <summary>
    /// 创建变量节点的辅助方法
    /// </summary>
    /// <param name="parent">父节点状态对象</param>
    /// <param name="name">变量名称</param>
    /// <param name="dataType">数据类型 ID</param>
    /// <param name="accessLevel">访问级别（读/写权限）</param>
    /// <returns>创建的变量状态对象</returns>
    private BaseDataVariableState CreateVariable(NodeState parent, string name, NodeId dataType, byte accessLevel)
    {
        // 【核心修改】生成全局唯一的 NodeId。
        // 父节点的名字是设备Id (例如 "Eq_1001")，所以我们可以合并为 "Eq_1001_温度"。
        // 这样即使几十个设备都有"温度"，它们的 NodeId 也是不同的。
        string uniqueNodeId = $"{parent.BrowseName.Name}_{name}";

        // 创建基础数据变量状态对象
        var variable = new BaseDataVariableState(parent)
        {
            // 节点 ID 使用 "Omron/名称" 格式，便于识别
            NodeId = new NodeId(uniqueNodeId, NamespaceIndex),
            // 浏览名称
            BrowseName = new QualifiedName(name, NamespaceIndex),
            // 显示名称
            DisplayName = new LocalizedText(name),
            // 数据类型（Int32 或 Float）
            DataType = dataType,
            // 值等级：标量（非数组）
            ValueRank = ValueRanks.Scalar,
            // 访问级别：决定客户端能否读写
            AccessLevel = accessLevel,
            // 用户访问级别：与访问级别相同
            UserAccessLevel = accessLevel,
            // 不记录历史数据
            Historizing = false,
            // 设置写入回调
            // 当 OPC UA 客户端写入此变量时，会调用 OnWriteVariable 方法
            OnSimpleWriteValue = OnWriteVariable
        };

        // 将变量添加为父节点的子节点
        parent.AddChild(variable);
        // 添加到预定义节点集合
        AddPredefinedNode(SystemContext, variable);

        return variable;
    }

    /// <summary>
    /// 变量写入回调 - 当 OPC UA 客户端写入变量时被调用
    /// </summary>
    /// <param name="context">系统上下文</param>
    /// <param name="node">被写入的节点</param>
    /// <param name="value">要写入的值（引用参数，可修改）</param>
    /// <returns>操作结果</returns>
    private ServiceResult OnWriteVariable(ISystemContext context, NodeState node, ref object value)
    {
        try
        {
            // 1. 通过节点 ID 找到它的物理映射路由
            string nodeIdStr = node.NodeId.Identifier.ToString() ?? string.Empty;

            if (!_nodeRoutingMap.TryGetValue(nodeIdStr, out var route))
            {
                // 如果找不到映射说明不是我们的设备点，或者找错了
                return StatusCodes.BadNodeIdUnknown;
            }

            // 2. 检查是否有应用层订阅了这个写事件
            if (OnOpcClientWriteRequestedAsync == null)
            {
                // 没有人监听，写入就不可能下发到 PLC
                return StatusCodes.BadInvalidState;
            }

            // 3. 将写入求发给外层，让外层去处理协议转换和驱动发送
            // (注意: OPC UA 这里的回调是同步的，所以我们要用 .GetAwaiter().GetResult() 阻塞等待)
            var (isSuccess, errorMsg) = OnOpcClientWriteRequestedAsync(route.Protocol, route.Equipment, route.Parameter, value).GetAwaiter().GetResult();

            if (isSuccess)
            {
                // 4. 写入 PLC 成功了！现在你可以把 OPC UA 内存模型里的值改掉，触发刷新
                // 【修复】加上 route.Parameter.DataType 进行类型转换
                UpdateVariableWithStatus((BaseDataVariableState)node, value, route.Parameter.DataType, StatusCodes.Good);
                return ServiceResult.Good;
            }
            else
            {
                // 5. 写入失败给客户端返回错误信息或设为 Bad状态
                // context.OperationContext...? 可以记录日志
                return new ServiceResult(StatusCodes.BadDeviceFailure, $"底层写入失败: {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    /// <summary>
    /// 将采集到或计算后的值，严格转换为点位配置中所要求的数据类型，防止 OPC UA 爆出 BadTypeMismatch 异常
    /// </summary>
    private static object? ConvertToExpectedType(object? value, DataType? expectedDataType)
    {
        if (value == null || expectedDataType == null) return value;

        try
        {
            // 利用 Convert.ToXXX 将算出来的 double 或其他类型，强制洗成目标原生类型
            // (例如：如果 value 是 10.5，要求 DataType 是 Int，会被强转为 10 或 11)
            return expectedDataType.Value switch
            {
                DataType.Bool => Convert.ToBoolean(value),
                DataType.Short => Convert.ToInt16(value),
                DataType.UShort => Convert.ToUInt16(value),
                DataType.Int => Convert.ToInt32(value),
                DataType.UInt => Convert.ToUInt32(value),
                DataType.Long => Convert.ToInt64(value),
                DataType.ULong => Convert.ToUInt64(value),
                DataType.Float => Convert.ToSingle(value),
                DataType.Double => Convert.ToDouble(value),
                DataType.String => Convert.ToString(value) ?? string.Empty,
                _ => value
            };
        }
        catch
        {
            return value; // 极端情况转换失败时，返回原值进行兜底
        }
    }
}
