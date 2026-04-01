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

    // ============== 状态节点缓存 ==============
    // 全局汇总状态节点
    private FolderState? _summaryFolder;
    private BaseDataVariableState? _lastUpdateTimeNode;
    private BaseDataVariableState? _totalProtocolsNode;
    private BaseDataVariableState? _healthyProtocolsNode;
    private BaseDataVariableState? _errorProtocolsNode;

    // 协议状态节点缓存
    private readonly Dictionary<string, FolderState> _protocolStatusFolders = new();
    private readonly Dictionary<string, BaseDataVariableState> _protocolStatusNodes = new();

    // 设备状态节点缓存
    private readonly Dictionary<string, FolderState> _equipmentStatusFolders = new();
    private readonly Dictionary<string, BaseDataVariableState> _equipmentStatusNodes = new();

    // 点位状态节点缓存
    private readonly Dictionary<string, FolderState> _pointStatusFolders = new();
    private readonly Dictionary<string, BaseDataVariableState> _pointStatusNodes = new();

    // 状态根节点
    private FolderState? _statusRootFolder;

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
        // -------------------------------------------------------
        // 树 1: Workstation 数据树
        //   - 用途：存放采集值，供原有客户端程序通过约定好的 NodeId 访问
        //   - 数据节点 NodeId 格式：{设备ID}_{参数Label}（如 E-001_温度）
        //   - 文件夹仅用于地址空间浏览，NodeId 使用路径格式（如 Workstation/E-001）
        // -------------------------------------------------------
        var workstationFolder = CreateFolder(ObjectIds.ObjectsFolder, "Workstation", externalReferences);

        // -------------------------------------------------------
        // 树 2: EquipmentStatus 状态树
        //   - 用途：存放采集状态信息（成功/失败/耗时/错误信息等）
        //   - 所有状态节点 NodeId 均使用路径格式（含 /），与数据节点（含 _）天然不冲突
        // -------------------------------------------------------
        _statusRootFolder = CreateFolder(ObjectIds.ObjectsFolder, "EquipmentStatus", externalReferences);

        // 创建全局汇总状态节点
        CreateSummaryStatusNodes(externalReferences);

        // 遍历配置，分别创建数据节点和状态节点
        foreach(var protocol in _config.Protocols)
        {
            CreateProtocolStatusNodes(protocol, externalReferences);

            foreach(var equipment in protocol.Equipments.Where(e => e.IsCollect))
            {
                if (equipment.Parameters == null || equipment.Parameters.Count == 0) continue;

                // 在 Workstation 树下为该设备创建文件夹（仅用于浏览，NodeId 路径格式不冲突）
                var equipmentDataFolder = CreateFolder(workstationFolder.NodeId, equipment.Id, externalReferences);

                // 在 EquipmentStatus 树下创建设备状态节点
                CreateEquipmentStatusNodes(protocol, equipment, externalReferences);

                foreach(var point in equipment.Parameters)
                {
                    // 创建数据值节点：NodeId = {设备ID}_{Label}（保留原始约定）
                    CreateLegacyDataValueNode(protocol, equipment, point, equipmentDataFolder);

                    // 创建状态信息节点：NodeId = EquipmentStatus/.../...（路径格式）
                    CreatePointStatusNodes(protocol, equipment, point, externalReferences);
                }
            }
        }
    }

    /// <summary>
    /// 创建数据值节点，使用与外部程序约定好的原始 NodeId 格式：{设备ID}_{参数Label}
    /// <para>例如：E-001_温度、E-002_压力</para>
    /// <para>此 NodeId 格式使用下划线（_），与状态节点的路径格式（/）天然不冲突</para>
    /// </summary>
    private void CreateLegacyDataValueNode(ProtocolConfig protocol, EquipmentConfig equipment, ParameterConfig point, FolderState equipmentFolder)
    {
        // 使用约定好的原始 NodeId 格式，确保与外部客户端程序的兼容性
        string legacyNodeId = $"{equipment.Id}_{point.Label}";
        NodeId opcDataType = MapToOpcDataType(point.DataType);

        var variable = new BaseDataVariableState(equipmentFolder)
        {
            NodeId = new NodeId(legacyNodeId, NamespaceIndex), // 原始 NodeId 格式，不使用路径继承
            BrowseName = new QualifiedName(point.Label, NamespaceIndex),
            DisplayName = new LocalizedText(point.Label),
            DataType = opcDataType,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = AccessLevels.CurrentReadOrWrite,
            Historizing = false,
            OnSimpleWriteValue = OnWriteVariable
        };

        equipmentFolder.AddChild(variable);
        AddPredefinedNode(SystemContext, variable);

        // 注册到缓存字典（供采集结果更新值，供 OnWriteVariable 路由下发）
        _pointNodes[legacyNodeId] = (variable, point.DataType);
        _nodeRoutingMap[legacyNodeId] = (protocol, equipment, point);

        // 设置初始占位值和 Bad 状态码，等待首次采集数据到达
        object defaultValue = GetDefaultValue(point.DataType);
        UpdateVariableWithStatus(variable, defaultValue, point.DataType, StatusCodes.BadWaitingForInitialData);
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
            // 更新时间戳为本地时间（SourceTimestamp）
            variable.Timestamp = DateTime.Now;

            //variable.Description = new LocalizedText("zh-CN", "测试的采集点状态");

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
        // 生成全局唯一的 NodeId：基于父节点路径 + 当前名称，防止同名文件夹产生重复 NodeId
        // 若父节点是 OPC UA 标准数字 ID（如 ObjectsFolder = 85），顶层节点直接用名称
        string uniqueId = parentId.IdType == IdType.String
            ? $"{parentId.Identifier}/{name}"
            : name;

        var folder = new FolderState(null)
        {
            NodeId = new NodeId(uniqueId, NamespaceIndex),
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
        // 使用父节点的 NodeId 路径生成全局唯一 NodeId，与 CreateFolder 保持一致的路径规则
        string parentPath = parent.NodeId.Identifier?.ToString() ?? parent.BrowseName.Name;
        string uniqueNodeId = $"{parentPath}/{name}";

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

    // ==================== 状态节点创建方法 ====================

    /// <summary>
    /// 创建全局汇总状态节点
    /// </summary>
    private void CreateSummaryStatusNodes(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        if (_statusRootFolder == null) return;

        _summaryFolder = CreateFolder(_statusRootFolder.NodeId, "Summary", externalReferences);

        _lastUpdateTimeNode = CreateStatusVariable(_summaryFolder, "LastUpdateTime", DataTypeIds.String, "等待更新");
        _totalProtocolsNode = CreateStatusVariable(_summaryFolder, "TotalProtocols", DataTypeIds.Int32, 0);
        _healthyProtocolsNode = CreateStatusVariable(_summaryFolder, "HealthyProtocols", DataTypeIds.Int32, 0);
        _errorProtocolsNode = CreateStatusVariable(_summaryFolder, "ErrorProtocols", DataTypeIds.Int32, 0);
    }

    /// <summary>
    /// 创建协议状态节点
    /// </summary>
    private void CreateProtocolStatusNodes(ProtocolConfig protocol, IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        if (_statusRootFolder == null) return;

        string protocolKey = $"{protocol.Id}";
        var protocolFolder = CreateFolder(_statusRootFolder.NodeId, protocolKey, externalReferences);
        _protocolStatusFolders[protocol.Id] = protocolFolder;

        // 协议级状态变量
        _protocolStatusNodes[$"{protocol.Id}_ReadIsSuccess"] = CreateStatusVariable(protocolFolder, "ReadIsSuccess", DataTypeIds.Boolean, false);
        _protocolStatusNodes[$"{protocol.Id}_ErrorMsg"] = CreateStatusVariable(protocolFolder, "ErrorMsg", DataTypeIds.String, "等待初始化");
        _protocolStatusNodes[$"{protocol.Id}_ElapsedMs"] = CreateStatusVariable(protocolFolder, "ElapsedMs", DataTypeIds.Int64, 0L);
        _protocolStatusNodes[$"{protocol.Id}_TotalEquipments"] = CreateStatusVariable(protocolFolder, "TotalEquipments", DataTypeIds.Int32, 0);
        _protocolStatusNodes[$"{protocol.Id}_SuccessEquipments"] = CreateStatusVariable(protocolFolder, "SuccessEquipments", DataTypeIds.Int32, 0);
        _protocolStatusNodes[$"{protocol.Id}_FailedEquipments"] = CreateStatusVariable(protocolFolder, "FailedEquipments", DataTypeIds.Int32, 0);
        _protocolStatusNodes[$"{protocol.Id}_TotalPoints"] = CreateStatusVariable(protocolFolder, "TotalPoints", DataTypeIds.Int32, 0);
        _protocolStatusNodes[$"{protocol.Id}_SuccessPoints"] = CreateStatusVariable(protocolFolder, "SuccessPoints", DataTypeIds.Int32, 0);
        _protocolStatusNodes[$"{protocol.Id}_FailedPoints"] = CreateStatusVariable(protocolFolder, "FailedPoints", DataTypeIds.Int32, 0);
        _protocolStatusNodes[$"{protocol.Id}_StartTime"] = CreateStatusVariable(protocolFolder, "StartTime", DataTypeIds.String, "-");
        _protocolStatusNodes[$"{protocol.Id}_EndTime"] = CreateStatusVariable(protocolFolder, "EndTime", DataTypeIds.String, "-");
        _protocolStatusNodes[$"{protocol.Id}_ProtocolType"] = CreateStatusVariable(protocolFolder, "ProtocolType", DataTypeIds.String, protocol.ProtocolType.ToString());
    }

    /// <summary>
    /// 创建设备状态节点
    /// </summary>
    private void CreateEquipmentStatusNodes(ProtocolConfig protocol, EquipmentConfig equipment, IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        if (_statusRootFolder == null) return;
        if (!_protocolStatusFolders.TryGetValue(protocol.Id, out var protocolFolder)) return;

        string equipmentKey = $"{protocol.Id}_{equipment.Id}";
        var equipmentFolder = CreateFolder(protocolFolder.NodeId, equipment.Id, externalReferences);
        _equipmentStatusFolders[equipmentKey] = equipmentFolder;

        // 设备级状态变量
        _equipmentStatusNodes[$"{equipmentKey}_ReadIsSuccess"] = CreateStatusVariable(equipmentFolder, "ReadIsSuccess", DataTypeIds.Boolean, false);
        _equipmentStatusNodes[$"{equipmentKey}_ErrorMsg"] = CreateStatusVariable(equipmentFolder, "ErrorMsg", DataTypeIds.String, "等待初始化");
        _equipmentStatusNodes[$"{equipmentKey}_ElapsedMs"] = CreateStatusVariable(equipmentFolder, "ElapsedMs", DataTypeIds.Int64, 0L);
        _equipmentStatusNodes[$"{equipmentKey}_TotalPoints"] = CreateStatusVariable(equipmentFolder, "TotalPoints", DataTypeIds.Int32, 0);
        _equipmentStatusNodes[$"{equipmentKey}_SuccessPoints"] = CreateStatusVariable(equipmentFolder, "SuccessPoints", DataTypeIds.Int32, 0);
        _equipmentStatusNodes[$"{equipmentKey}_FailedPoints"] = CreateStatusVariable(equipmentFolder, "FailedPoints", DataTypeIds.Int32, 0);
        _equipmentStatusNodes[$"{equipmentKey}_StartTime"] = CreateStatusVariable(equipmentFolder, "StartTime", DataTypeIds.String, "-");
        _equipmentStatusNodes[$"{equipmentKey}_EndTime"] = CreateStatusVariable(equipmentFolder, "EndTime", DataTypeIds.String, "-");
        _equipmentStatusNodes[$"{equipmentKey}_EquipmentName"] = CreateStatusVariable(equipmentFolder, "EquipmentName", DataTypeIds.String, equipment.Name ?? equipment.Id);

        // 创建点位状态文件夹
        var pointsFolder = CreateFolder(equipmentFolder.NodeId, "Points", externalReferences);
        _pointStatusFolders[equipmentKey] = pointsFolder;
    }

    /// <summary>
    /// 创建点位状态节点（仅包含状态信息，不再重复创建数据值节点）
    /// <para>数据值节点由 CreateLegacyDataValueNode 在 Workstation 树下创建，此处只创建辅助状态信息</para>
    /// </summary>
    private void CreatePointStatusNodes(ProtocolConfig protocol, EquipmentConfig equipment, ParameterConfig point, IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        string equipmentKey = $"{protocol.Id}_{equipment.Id}";
        if (!_pointStatusFolders.TryGetValue(equipmentKey, out var pointsFolder)) return;

        string pointKey = $"{equipmentKey}_{point.Label}";
        var pointFolder = CreateFolder(pointsFolder.NodeId, point.Label, externalReferences);

        // ========== 状态信息节点（只读）==========
        // 【关键修复】不再创建数据值节点，避免覆盖 CreateLegacyDataValueNode 中注册的正确节点
        // 数据值由 Workstation 树下的节点（NodeId 格式：{设备ID}_{参数Label}）提供
        _pointStatusNodes[$"{pointKey}_ReadIsSuccess"] = CreateStatusVariable(pointFolder, "ReadIsSuccess", DataTypeIds.Boolean, false);
        _pointStatusNodes[$"{pointKey}_ErrorMsg"] = CreateStatusVariable(pointFolder, "ErrorMsg", DataTypeIds.String, "等待初始化");
        _pointStatusNodes[$"{pointKey}_ElapsedMs"] = CreateStatusVariable(pointFolder, "ElapsedMs", DataTypeIds.Int64, 0L);
        _pointStatusNodes[$"{pointKey}_DataType"] = CreateStatusVariable(pointFolder, "DataType", DataTypeIds.String, point.DataType?.ToString() ?? "Unknown");
        _pointStatusNodes[$"{pointKey}_Address"] = CreateStatusVariable(pointFolder, "Address", DataTypeIds.String, point.Address ?? "-");

        // 【新增】创建一个只读的 Value 镜像节点，用于在状态树中查看当前采集值
        // 此节点使用独立的路径格式 NodeId，不会与 Workstation 树的数据节点冲突
        NodeId opcDataType = MapToOpcDataType(point.DataType);
        var valueNode = CreateStatusVariable(pointFolder, "Value", opcDataType, GetDefaultValue(point.DataType));
        // 将镜像节点存入点位状态字典，供状态更新时同步值
        _pointStatusNodes[$"{pointKey}_Value"] = valueNode;
    }

    /// <summary>
    /// 创建状态变量的辅助方法
    /// </summary>
    private BaseDataVariableState CreateStatusVariable(NodeState parent, string name, NodeId dataType, object initialValue)
    {
        // 使用父节点的 NodeId 路径生成全局唯一 NodeId，与其他节点保持一致的路径规则
        string parentPath = parent.NodeId.Identifier?.ToString() ?? parent.BrowseName.Name;
        var variable = new BaseDataVariableState(parent)
        {
            NodeId = new NodeId($"{parentPath}/{name}", NamespaceIndex),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = new LocalizedText(name),
            DataType = dataType,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = AccessLevels.CurrentReadOrWrite,
            Value = initialValue
        };

        parent.AddChild(variable);
        AddPredefinedNode(SystemContext, variable);
        return variable;
    }

    // ==================== 状态更新方法 ====================

    /// <summary>
    /// 更新数据采集结果到状态节点（扩展版本，包含完整状态信息）
    /// </summary>
    public void UpdateDataFromCollectionResult(ProtocolResult result)
    {
        lock (Lock)
        {
            // 1. 更新数据节点（原有逻辑）
            UpdateDataNodes(result);

            // 2. 更新协议状态节点
            UpdateProtocolStatus(result);

            // 3. 更新设备状态节点
            foreach (var eqResult in result.EquipmentResults)
            {
                UpdateEquipmentStatus(result.ProtocolId, eqResult);
            }

            // 4. 更新全局汇总状态
            UpdateSummaryStatus(result);
        }
    }

    /// <summary>
    /// 更新数据节点（原有逻辑抽取）
    /// </summary>
    private void UpdateDataNodes(ProtocolResult result)
    {
        // 协议级彻底失败
        if (result.AllEquipmentsFailed())
        {
            MarkProtocolNodesAsBad(result.ProtocolId, StatusCodes.BadCommunicationError);
            return;
        }

        // 遍历设备结果
        foreach (var eqResult in result.EquipmentResults)
        {
            if (eqResult.AllPointsFailed())
            {
                MarkEquipmentNodesAsBad(eqResult.EquipmentId, StatusCodes.BadNotConnected);
                continue;
            }

            // 遍历点位结果
            foreach (var ptResult in eqResult.PointResults)
            {
                string cacheKey = $"{eqResult.EquipmentId}_{ptResult.Label}";
                if (_pointNodes.TryGetValue(cacheKey, out var nodeInfo))
                {
                    var (variableNode, declaredDataType) = nodeInfo;
                    if (ptResult.ReadIsSuccess && ptResult.Value != null)
                    {
                        UpdateVariableWithStatus(variableNode, ptResult.Value, declaredDataType, StatusCodes.Good);
                    }
                    else
                    {
                        UpdateVariableWithStatus(variableNode, variableNode.Value, declaredDataType, StatusCodes.Bad);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 更新协议状态节点
    /// </summary>
    private void UpdateProtocolStatus(ProtocolResult result)
    {
        string protocolId = result.ProtocolId;

        UpdateStatusNode($"{protocolId}_ReadIsSuccess", result.ReadIsSuccess);
        UpdateStatusNode($"{protocolId}_ErrorMsg", result.ErrorMsg ?? (result.ReadIsSuccess ? "成功" : "失败"));
        UpdateStatusNode($"{protocolId}_ElapsedMs", result.ElapsedMs);
        UpdateStatusNode($"{protocolId}_TotalEquipments", result.TotalEquipments);
        UpdateStatusNode($"{protocolId}_SuccessEquipments", result.SuccessEquipments);
        UpdateStatusNode($"{protocolId}_FailedEquipments", result.FailedEquipments);
        UpdateStatusNode($"{protocolId}_TotalPoints", result.TotalPoints);
        UpdateStatusNode($"{protocolId}_SuccessPoints", result.SuccessPoints);
        UpdateStatusNode($"{protocolId}_FailedPoints", result.FailedPoints);
        UpdateStatusNode($"{protocolId}_StartTime", result.StartTime);
        UpdateStatusNode($"{protocolId}_EndTime", result.EndTime);
    }

    /// <summary>
    /// 更新设备状态节点
    /// </summary>
    private void UpdateEquipmentStatus(string protocolId, EquipmentResult eqResult)
    {
        string equipmentKey = $"{protocolId}_{eqResult.EquipmentId}";

        UpdateStatusNode($"{equipmentKey}_ReadIsSuccess", eqResult.ReadIsSuccess);
        UpdateStatusNode($"{equipmentKey}_ErrorMsg", eqResult.ErrorMsg ?? (eqResult.ReadIsSuccess ? "成功" : "失败"));
        UpdateStatusNode($"{equipmentKey}_ElapsedMs", eqResult.ElapsedMs);
        UpdateStatusNode($"{equipmentKey}_TotalPoints", eqResult.TotalPoints);
        UpdateStatusNode($"{equipmentKey}_SuccessPoints", eqResult.SuccessPoints);
        UpdateStatusNode($"{equipmentKey}_FailedPoints", eqResult.FailedPoints);
        UpdateStatusNode($"{equipmentKey}_StartTime", eqResult.StartTime);
        UpdateStatusNode($"{equipmentKey}_EndTime", eqResult.EndTime);

        // 更新点位状态
        foreach (var ptResult in eqResult.PointResults)
        {
            UpdatePointStatus(equipmentKey, ptResult);
        }
    }

    /// <summary>
    /// 更新点位状态节点
    /// </summary>
    private void UpdatePointStatus(string equipmentKey, PointResult ptResult)
    {
        string pointKey = $"{equipmentKey}_{ptResult.Label}";

        UpdateStatusNode($"{pointKey}_ReadIsSuccess", ptResult.ReadIsSuccess);
        UpdateStatusNode($"{pointKey}_ErrorMsg", ptResult.ErrorMsg ?? (ptResult.ReadIsSuccess ? "成功" : "失败"));
        UpdateStatusNode($"{pointKey}_ElapsedMs", ptResult.ElapsedMs);
        UpdateStatusNode($"{pointKey}_DataType", ptResult.DataType?.ToString() ?? "Unknown");

        // 【新增】同步更新状态树中的 Value 镜像节点（只读副本）
        // 这样在 EquipmentStatus 树中也能看到当前采集值，方便诊断
        if (ptResult.ReadIsSuccess && ptResult.Value != null)
        {
            // 将值转换为预期类型后更新镜像节点
            var convertedValue = ConvertToExpectedType(ptResult.Value, ptResult.DataType);
            UpdateStatusNode($"{pointKey}_Value", convertedValue ?? GetDefaultValue(ptResult.DataType));
        }
    }

    /// <summary>
    /// 更新全局汇总状态
    /// </summary>
    private void UpdateSummaryStatus(ProtocolResult result)
    {
        if (_lastUpdateTimeNode != null)
        {
            _lastUpdateTimeNode.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _lastUpdateTimeNode.ClearChangeMasks(SystemContext, false);
        }

        // 计算协议统计（这里简化处理，实际可能需要跨多次调用维护状态）
        if (_totalProtocolsNode != null)
        {
            _totalProtocolsNode.Value = _config.Protocols.Count;
            _totalProtocolsNode.ClearChangeMasks(SystemContext, false);
        }

        // 根据当前结果更新健康/错误协议数
        if (_healthyProtocolsNode != null && _errorProtocolsNode != null)
        {
            int healthy = result.ReadIsSuccess ? 1 : 0;
            int error = result.ReadIsSuccess ? 0 : 1;
            _healthyProtocolsNode.Value = healthy;
            _errorProtocolsNode.Value = error;
            _healthyProtocolsNode.ClearChangeMasks(SystemContext, false);
            _errorProtocolsNode.ClearChangeMasks(SystemContext, false);
        }
    }

    /// <summary>
    /// 更新状态节点值的通用方法
    /// </summary>
    private void UpdateStatusNode(string key, object value)
    {
        if (_protocolStatusNodes.TryGetValue(key, out var node))
        {
            node.Value = value;
            node.Timestamp = DateTime.Now;
            node.StatusCode = StatusCodes.Good;
            node.ClearChangeMasks(SystemContext, false);
            return;
        }

        if (_equipmentStatusNodes.TryGetValue(key, out node))
        {
            node.Value = value;
            node.Timestamp = DateTime.Now;
            node.StatusCode = StatusCodes.Good;
            node.ClearChangeMasks(SystemContext, false);
            return;
        }

        if (_pointStatusNodes.TryGetValue(key, out node))
        {
            node.Value = value;
            node.Timestamp = DateTime.Now;
            node.StatusCode = StatusCodes.Good;
            node.ClearChangeMasks(SystemContext, false);
        }
    }
}
