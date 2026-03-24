namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.Sql;

/// <summary>
/// SQLite 协议驱动
/// 所有读取逻辑均由 BaseDatabaseDriver 提供，本类仅附加协议名称语义：
///   SQLiteDriver.GetProtocolName() = "SQLite" → 匹配 ProtocolType.SQLite.ToString()
/// </summary>
public class SQLiteDriver : BaseDatabaseDriver { }
