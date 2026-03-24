using IndustrialDataProcessor.Domain.Attributes;
using System.ComponentModel;

namespace IndustrialDataProcessor.Domain.Enums;

/// <summary>
/// 协议类型
/// </summary>
public enum ProtocolType
{
    #region LAN
    /// <summary>
    /// ModbusTcpNet
    /// </summary>
    [Description("ModbusTcpNet")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: true, requireDataFormat: true, requireDataType: true, requireAddressStartWithZero: true, requireInstrumentType: false)]
    ModbusTcpNet = 0,

    /// <summary>
    /// ModbusRtuOverTcp
    /// </summary>
    [Description("ModbusRtuOverTcp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: true, requireDataFormat: true, requireDataType: true, requireAddressStartWithZero: true, requireInstrumentType: false)]
    ModbusRtuOverTcp = 1,

    /// <summary>
    /// OmronFinsNet
    /// </summary>
    [Description("OmronFinsTcp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    OmronFinsTcp = 2,

    /// <summary>
    /// OmronCipNet
    /// </summary>
    [Description("OmronCipNet")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    OmronCipNet = 3,

    /// <summary>
    /// SiemensS200Smart
    /// </summary>
    [Description("SiemensS200Smart")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    SiemensS200Smart = 4,

    /// <summary>
    /// SiemensS1200
    /// </summary>
    [Description("SiemensS1200")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    SiemensS1200 = 5,

    /// <summary>
    /// SiemensS1500
    /// </summary>
    [Description("SiemensS1500")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    SiemensS1500 = 6,

    /// <summary>
    /// SiemensS200
    /// </summary>
    [Description("SiemensS200")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    SiemensS200 = 7,

    /// <summary>
    /// SiemensS300
    /// </summary>
    [Description("SiemensS300")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    SiemensS300 = 8,

    /// <summary>
    /// SiemensS400
    /// </summary>
    [Description("SiemensS400")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    SiemensS400 = 9,

    /// <summary>
    /// DLT6452007OverTcp
    /// </summary>
    [Description("DLT6452007OverTcp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: true, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    DLT6452007OverTcp = 10,

    /// <summary>
    /// CJT1882004OverTcp
    /// </summary>
    [Description("CJT1882004OverTcp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: true, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: true)]
    CJT1882004OverTcp = 11,

    /// <summary>
    /// FxSerialOverTcp
    /// </summary>
    [Description("FxSerialOverTcp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    FxSerialOverTcp = 12,

    /// <summary>
    /// IEC104
    /// </summary>
    [Description("IEC104")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: false, requireAddressStartWithZero: false, requireInstrumentType: false)]
    IEC104 = 13,

    /// <summary>
    /// OpcUa
    /// </summary>
    [Description("OpcUa")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: false, requireAddressStartWithZero: false, requireInstrumentType: false)]
    OpcUa = 14,

    #region Udp
    /// <summary>
    /// OmronFinsUdp
    /// </summary>
    [Description("OmronFinsUdp")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    OmronFinsUdp = 15,
    #endregion

    #region 自由协议
    /// <summary>
    /// FJ1000Jet
    /// </summary>
    [Description("FJ1000Jet")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: false, requireAddressStartWithZero: false, requireInstrumentType: false)]
    FJ1000Jet = 16,

    /// <summary>
    /// FJ60W
    /// </summary>
    [Description("FJ60W")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: false, requireAddressStartWithZero: false, requireInstrumentType: false)]
    FJ60W = 17,

    /// <summary>
    /// GP1125T
    /// </summary>
    [Description("GP1125T")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: false, requireAddressStartWithZero: false, requireInstrumentType: false)]
    GP1125T = 18,

    /// <summary>
    /// BottomImageF1970
    /// </summary>
    [Description("BottomImageF1970")]
    [ProtocolInterfaceType(InterfaceType.LAN)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: false, requireAddressStartWithZero: false, requireInstrumentType: false)]
    BottomImageF1970 = 19,
    #endregion

    #endregion

    #region COM
    /// <summary>
    /// ModbusRtu
    /// </summary>
    [Description("ModbusRtu")]
    [ProtocolInterfaceType(InterfaceType.COM)]
    [ProtocolValidateParameter(requireStationNo: true, requireDataFormat: true, requireDataType: true, requireAddressStartWithZero: true, requireInstrumentType: false)]
    ModbusRtu = 100,

    /// <summary>
    /// DLT6452007Serial
    /// </summary>
    [Description("DLT6452007Serial")]
    [ProtocolInterfaceType(InterfaceType.COM)]
    [ProtocolValidateParameter(requireStationNo: true, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    DLT6452007Serial = 101,

    /// <summary>
    /// CJT1882004Serial
    /// </summary>
    [Description("CJT1882004Serial")]
    [ProtocolInterfaceType(InterfaceType.COM)]
    [ProtocolValidateParameter(requireStationNo: true, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: true)]
    CJT1882004Serial = 102,

    /// <summary>
    /// FxSerial
    /// </summary>
    [Description("FxSerial")]
    [ProtocolInterfaceType(InterfaceType.COM)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: true, requireAddressStartWithZero: false, requireInstrumentType: false)]
    FxSerial = 103,
    #endregion

    #region API
    /// <summary>
    /// Api
    /// </summary>
    [Description("Api")]
    [ProtocolInterfaceType(InterfaceType.API)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: false, requireAddressStartWithZero: false, requireInstrumentType: false)]
    Api = 200,
    #endregion

    #region DATABASE
    /// <summary>
    /// MySQL
    /// </summary>
    [Description("MySQL")]
    [ProtocolInterfaceType(InterfaceType.DATABASE)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: false, requireAddressStartWithZero: false, requireInstrumentType: false)]
    MySQL = 300,

    /// <summary>
    /// SQLite
    /// </summary>
    [Description("SQLite")]
    [ProtocolInterfaceType(InterfaceType.DATABASE)]
    [ProtocolValidateParameter(requireStationNo: false, requireDataFormat: false, requireDataType: false, requireAddressStartWithZero: false, requireInstrumentType: false)]
    SQLite = 301,
    #endregion
}