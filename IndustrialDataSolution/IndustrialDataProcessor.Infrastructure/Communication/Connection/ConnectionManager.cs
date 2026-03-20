using HslCommunication.Instrument.CJT;
using HslCommunication.Instrument.DLT;
using HslCommunication.ModBus;
using HslCommunication.Profinet.Melsec;
using HslCommunication.Profinet.Omron;
using HslCommunication.Profinet.Siemens;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
using IndustrialDataProcessor.Infrastructure.Communication.Drivers.TcpSpecial;
using lib60870.CS101;
using lib60870.CS104;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Text;

namespace IndustrialDataProcessor.Infrastructure.Communication.Connection;

public class ConnectionManager : IConnectionManager, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, IConnectionHandle> _connections = [];

    public async Task<IConnectionHandle> GetOrCreateConnectionAsync(ProtocolConfig config, CancellationToken token)
    {
        var channelKey = config.Id;

        // 如果连接已存在，直接返回复用
        if (_connections.TryGetValue(channelKey, out var existingHandle))
            return existingHandle;

        // 创建新连接
        var newHandle = await CreateHandleAsync(config, token);
        return _connections.GetOrAdd(channelKey, newHandle);
    }

    private async Task<IConnectionHandle> CreateHandleAsync(ProtocolConfig config, CancellationToken token)
    {
        // 1. 先按接口类型 (InterfaceType) 分类分发
        if (config is NetworkProtocolConfig netConfig)
        {
            // LAN 接口
            return await CreateLanConnectionAsync(netConfig, token);
        }
        else if (config is SerialPortConfig serialConfig)
        {
            // COM 接口
            return await CreateComConnectionAsync(serialConfig, token);
        }
        else if (config is HttpApiInterfaceConfig apiConfig)
        {
            // API 接口
            return await CreateApiConnectionAsync(apiConfig, token);
        }
        else
        {
            // DATABASE 或其他接口处理 (可按需在这里扩展 config is DatabaseInterfaceConfig 等)
            throw new NotSupportedException($"暂不支持的接口配置类型: {config.GetType().Name}");
        }
    }

    /// <summary>
    /// 处理网口(LAN)的协议连接
    /// </summary>
    private async Task<IConnectionHandle> CreateLanConnectionAsync(NetworkProtocolConfig netConfig, CancellationToken token)
    {
        // 2. 按具体协议类型 (ProtocolType) 分类处理
        switch (netConfig.ProtocolType) // 假设配置类中存在此属性
        {
            case ProtocolType.ModbusTcpNet:
                {
                    var rawConn = new ModbusTcpNet(netConfig.IpAddress, netConfig.ProtocolPort)
                    {
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 Modbus设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.ModbusRtuOverTcp:
                {
                    var rawConn = new ModbusRtuOverTcp(netConfig.IpAddress, netConfig.ProtocolPort)
                    {
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 Modbus设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.OmronFinsTcp:
                {
                    var rawConn = new OmronFinsNet(netConfig.IpAddress, netConfig.ProtocolPort)
                    {
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.OmronCipNet:
                {
                    var rawConn = new OmronCipNet(netConfig.IpAddress, netConfig.ProtocolPort)
                    {
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.SiemensS200Smart:
                {
                    var rawConn = new SiemensS7Net(SiemensPLCS.S200Smart, netConfig.IpAddress)
                    {
                        Port = netConfig.ProtocolPort,
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.SiemensS1200:
                {
                    var rawConn = new SiemensS7Net(SiemensPLCS.S1200, netConfig.IpAddress)
                    {
                        Port = netConfig.ProtocolPort,
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.SiemensS1500:
                {
                    var rawConn = new SiemensS7Net(SiemensPLCS.S1500, netConfig.IpAddress)
                    {
                        Port = netConfig.ProtocolPort,
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.SiemensS200:
                {
                    var rawConn = new SiemensS7Net(SiemensPLCS.S200, netConfig.IpAddress)
                    {
                        Port = netConfig.ProtocolPort,
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.SiemensS300:
                {
                    var rawConn = new SiemensS7Net(SiemensPLCS.S300, netConfig.IpAddress)
                    {
                        Port = netConfig.ProtocolPort,
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.SiemensS400:
                {
                    var rawConn = new SiemensS7Net(SiemensPLCS.S400, netConfig.IpAddress)
                    {
                        Port = netConfig.ProtocolPort,
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.DLT6452007OverTcp:
                {
                    var rawConn = new DLT645OverTcp(netConfig.IpAddress, netConfig.ProtocolPort)
                    {
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.CJT1882004OverTcp:
                {
                    var rawConn = new CJT188OverTcp("1")
                    {
                        IpAddress = netConfig.IpAddress,
                        Port = netConfig.ProtocolPort,
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.FxSerialOverTcp:
                {
                    var rawConn = new MelsecFxSerialOverTcp(netConfig.IpAddress, netConfig.ProtocolPort)
                    {
                        ConnectTimeOut = netConfig.ConnectTimeOut,
                        ReceiveTimeOut = netConfig.ReceiveTimeOut
                    };
                    var rawConnRes = await rawConn.ConnectServerAsync();
                    if (!rawConnRes.IsSuccess) throw new Exception($"连接 OmronFins设备 {netConfig.IpAddress} 失败: {rawConnRes.Message}");
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.OmronFinsUdp:
                {
                    var rawConn = new OmronFinsUdp()
                    {
                        CommunicationPipe = new HslCommunication.Core.Pipe.PipeUdpNet(netConfig.IpAddress, netConfig.ProtocolPort)
                        {
                            ReceiveTimeOut = netConfig.ReceiveTimeOut,    // 接收设备数据反馈的超时时间
                            SleepTime = 0,
                            SocketKeepAliveTime = -1,
                            IsPersistentConnection = true,
                        },
                        PlcType = OmronPlcType.CSCJ,
                        SA1 = 1,
                        GCT = 2,
                        DA1 = 0
                    };

                    rawConn.ByteTransform.DataFormat = HslCommunication.Core.DataFormat.CDAB;
                    rawConn.ByteTransform.IsStringReverseByteWord = true;
                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.OpcUa:
                {
                    var config = new ApplicationConfiguration()
                    {
                        ApplicationName = "MyClient",
                        ApplicationUri = Utils.Format(@"urn:{0}:MyClient", System.Net.Dns.GetHostName()),
                        ApplicationType = ApplicationType.Client,
                        SecurityConfiguration = new SecurityConfiguration
                        {
                            ApplicationCertificate = new CertificateIdentifier
                            {
                                StoreType = "Directory",
                                StorePath = "./CertificateStores/MachineDefault",
                                SubjectName = "MyClientSubjectName"
                            },
                            TrustedIssuerCertificates = new CertificateTrustList
                            {
                                StoreType = "Directory",
                                StorePath = "./CertificateStores/UA Certificate Authorities"
                            },
                            TrustedPeerCertificates = new CertificateTrustList
                            {
                                StoreType = "Directory",
                                StorePath = "./CertificateStores/UA Applications"
                            },
                            RejectedCertificateStore = new CertificateTrustList
                            {
                                StoreType = "Directory",
                                StorePath = "./CertificateStores/RejectedCertificates"
                            },
                            AutoAcceptUntrustedCertificates = true,
                            RejectSHA1SignedCertificates = false,
                            MinimumCertificateKeySize = 1024,
                            NonceLength = 32,
                        },
                        TransportConfigurations = new TransportConfigurationCollection(),
                        TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                        ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                        TraceConfiguration = new TraceConfiguration()
                    };

                    await config.ValidateAsync(ApplicationType.Client);

                    // 设置证书验证事件，用于自动接受不受信任的证书
                    if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                    {
                        config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
                    }

                    // 创建一个应用实例对象，用于检查证书
                    var application = new ApplicationInstance(config, null);

                    // 检查应用实例对象的证书
                    bool check = await application.CheckApplicationInstanceCertificatesAsync(false, 12, token);

                    string url = $"opc.tcp://{netConfig.IpAddress}:{netConfig.ProtocolPort}";

                    // 创建一个会话对象，用于连接到 OPC UA 服务器
                    var endpointDescription = await CoreClientUtils.SelectEndpointAsync(config, url, false, null!, CancellationToken.None) ?? throw new Exception($"连接 OPC UA 设备 {netConfig.IpAddress} 失败: 未能找到有效的 Endpoint。");

                    var endpointConfiguration = EndpointConfiguration.Create(config);

                    var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

                    UserIdentity userIdentity;
                    // 在 Session.Create 调用之前添加日志或验证
                    if (string.IsNullOrWhiteSpace(netConfig.Account) || string.IsNullOrWhiteSpace(netConfig.Password))
                        userIdentity = new UserIdentity();
                    else
                    {
                        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(netConfig.Password ?? "");
                        userIdentity = new UserIdentity(netConfig.Account, passwordBytes);
                    }


                    var sessionFactory = new DefaultSessionFactory(null!);
                    var rawConn = await sessionFactory.CreateAsync(config, endpoint, false, false, "DataCollector", 60000, userIdentity, null, token);

                    return new DefaultConnectionHandle(rawConn);
                }

            case ProtocolType.IEC104:
                {
                    var client = new Iec104Client(netConfig.IpAddress, netConfig.ProtocolPort);
                    client.Connect(); // 此处可加入重试或异常捕获等逻辑
                    return new DefaultConnectionHandle(client);
                }

            // TODO: 继续添加其它通过 LAN 通讯的协议分类 (如 Siemens1200 等...)
            // case ProtocolType.SiemensS1200: 
            //    return ...

            default:
                throw new NotSupportedException($"不支持的 LAN 协议类型: {netConfig.ProtocolType}");
        }
    }

    /// <summary>
    /// 处理串口(COM)的协议连接
    /// </summary>
    private async Task<IConnectionHandle> CreateComConnectionAsync(SerialPortConfig serialConfig, CancellationToken token)
    {
        // 2. 按具体协议类型 (ProtocolType) 分类处理
        switch (serialConfig.ProtocolType) // 假设配置类中存在此属性
        {
            case ProtocolType.ModbusRtu:
                var modbusRtu = new ModbusRtu();
                // 这里需根据您的 SerialProtocolConfig 补充波特率、端口号等赋值逻辑
                // modbusRtu.SerialPortInni(serialConfig.PortName, serialConfig.BaudRate, ...);
                // 由于是串口大多为同步开启，具体依赖 HSL 库中的接口实现
                // modbusRtu.Open();

                return new DefaultConnectionHandle(modbusRtu);

            // TODO: 继续添加其它通过 COM 通讯的协议分类
            default:
                throw new NotSupportedException($"不支持的 COM 协议类型: {serialConfig.ProtocolType}");
        }
    }

    /// <summary>
    /// 处理 HTTP API 的协议连接
    /// </summary>
    private Task<IConnectionHandle> CreateApiConnectionAsync(HttpApiInterfaceConfig apiConfig, CancellationToken token)
    {
        // 创建 HttpClient 并配置
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(apiConfig.ReceiveTimeOut > 0 ? apiConfig.ReceiveTimeOut : 10000)
        };

        // 设置默认请求头
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // 如果有账号密码，添加Basic认证
        if (!string.IsNullOrWhiteSpace(apiConfig.Account) && !string.IsNullOrWhiteSpace(apiConfig.Password))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiConfig.Account}:{apiConfig.Password}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        // 创建连接句柄
        var handle = new HttpApiConnectionHandle(
            httpClient,
            apiConfig.AccessApiString,
            apiConfig.RequestMethod ?? RequestMethod.Get,
            apiConfig.Account,
            apiConfig.Password,
            apiConfig.Gateway
        );

        return Task.FromResult<IConnectionHandle>(handle);
    }

    public async Task ClearAllConnectionsAsync()
    {
        // 1. 将现有的所有连接拿出
        var handlesToDispose = _connections.Values.ToList();

        // 2. 清空字典 (保证后续请求只能去建立新连接)
        _connections.Clear();

        // 3. 释放旧的连接资源
        foreach (var handle in handlesToDispose)
        {
            await handle.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var handle in _connections.Values)
        {
            await handle.DisposeAsync();
        }
        _connections.Clear();
    }
}
