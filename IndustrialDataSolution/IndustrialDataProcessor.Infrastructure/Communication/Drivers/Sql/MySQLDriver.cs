namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.Sql;

/// <summary>
/// MySQL 协议驱动
/// 所有读取逻辑均由 BaseDatabaseDriver 提供，本类仅附加协议名称语义ï¼
///   MySQLDriver.GetProtocolName() = "MySQL" → 匹配 ProtocolType.MySQL.ToString()
/// </summary>
public class MySQLDriver : BaseDatabaseDriver { }
