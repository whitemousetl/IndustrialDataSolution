using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using IndustrialDataProcessor.Contracts.WorkstationDto;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.WpfClient.Interfaces;
using IndustrialDataProcessor.WpfClient.Services;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class WorkstationConfigViewModel : ObservableValidator, IConfigSaveable
{
    private readonly IWorkstationConfigService _configService;

    #region 构造函数
    public WorkstationConfigViewModel() : this(new WorkstationConfigService())
    {
    }

    public WorkstationConfigViewModel(IWorkstationConfigService configService)
    {
        _configService = configService;

        // 默认添加一个协议配置
        AddProtocolCommand.Execute(null);
    }
    #endregion

    #region 页面状态

    [ObservableProperty]
    private string _pageTitle = "工作站配置管理";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    #endregion

    #region 工作站基础信息

    [ObservableProperty]
    [Required(ErrorMessage = "工作站ID不能为空")]
    [MaxLength(50, ErrorMessage = "工作站ID不能超过50个字符")]
    private string _workstationId = "ws-001";

    [ObservableProperty]
    [Required(ErrorMessage = "工作站ID不能为空")]
    [MaxLength(100, ErrorMessage = "工作站ID不能超过100个字符")]
    private string _workstationName = "工作站1";

    [ObservableProperty]
    [Required(ErrorMessage = "IP地址不能为空")]
    [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$", ErrorMessage = "IP地址格式不正确")]
    private string _ipAddress = "192.168.1.100";

    #endregion

    #region 协议配置

    [ObservableProperty]
    private ObservableCollection<ProtocolConfigDto> _protocols = [];

    [ObservableProperty]
    private ProtocolConfigDto? _selectedProtocol;

    #endregion

    #region 协议类型选项

    // 接口类型选项（用于下拉框）
    public Array InterfaceTypes => Enum.GetValues(typeof(InterfaceType));

    // 协议类型选项（根据接口类型过滤）
    [ObservableProperty]
    private List<ProtocolType> _availableProtocolTypes = new();

    // 波特率选项
    public Array BaudRateTypes => Enum.GetValues(typeof(BaudRateType));

    // 数据位选项
    public Array DataBitsTypes => Enum.GetValues(typeof(DataBitsType));

    // 校验位选项
    public Array ParityTypes => Enum.GetValues(typeof(DomainParity));

    // 停止位选项
    public Array StopBitsTypes => Enum.GetValues(typeof(DomainStopBits));

    // 请求方法选项
    public Array RequestMethods => Enum.GetValues(typeof(RequestMethod));

    #endregion

    #region 命令

    [RelayCommand]
    private async Task LoadConfig()
    {
        IsLoading = true;
        StatusMessage = "正在加载配置...";

        try
        {
            var config = await _configService.GetConfigAsync();
            if (config != null)
            {
                WorkstationId = config.Id ?? "WS-001";
                WorkstationName = config.Name ?? "工作站1";
                IpAddress = config.IpAddress ?? "192.168.1.100";
                Protocols = new ObservableCollection<ProtocolConfigDto>(config.Protocols);
                StatusMessage = "配置加载成功";
                Growl.Success("配置加载成功");
            }
            else
            {
                StatusMessage = "未找到已保存的配置，使用默认值";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            Growl.Error($"加载配置失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveConfig()
    {
        if (string.IsNullOrWhiteSpace(WorkstationId))
        {
            Growl.Warning("请输入工作站ID");
            return;
        }

        if (string.IsNullOrWhiteSpace(WorkstationName))
        {
            Growl.Warning("请输入工作站名称");
            return;
        }

        if (Protocols.Count == 0)
        {
            Growl.Warning("请至少添加一个协议配置");
            return;
        }

        IsLoading = true;
        StatusMessage = "正在保存配置...";

        try
        {
            var config = new WorkstationConfigDto
            {
                Id = WorkstationId,
                Name = WorkstationName,
                IpAddress = IpAddress,
                Protocols = Protocols.ToList()
            };

            var success = await _configService.SaveConfigAsync(config);
            if (success)
            {
                StatusMessage = "配置保存成功";
                Growl.Success("配置保存成功，后台服务正在更新");
            }
            else
            {
                StatusMessage = "配置保存失败";
                Growl.Error("配置保存失败，请检查后端服务是否正常运行");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
            Growl.Error($"保存配置失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddProtocol()
    {
        var newProtocol = new ProtocolConfigDto
        {
            Id = $"P-{Protocols.Count + 1:D3}",
            InterfaceType = InterfaceType.LAN,
            ProtocolType = ProtocolType.ModbusTcpNet,
            CommunicationDelay = 50000,
            ReceiveTimeOut = 10000,
            ConnectTimeOut = 10000
        };
        Protocols.Add(newProtocol);
        SelectedProtocol = newProtocol;
        UpdateAvailableProtocolTypes(newProtocol.InterfaceType);
    }

    [RelayCommand]
    private void RemoveProtocol(ProtocolConfigDto? protocol)
    {
        if (protocol != null)
        {
            Protocols.Remove(protocol);
        }
    }

    [RelayCommand]
    private void DuplicateProtocol(ProtocolConfigDto? protocol)
    {
        if (protocol == null) return;

        var duplicated = new ProtocolConfigDto
        {
            Id = $"{protocol.Id}-副本",
            InterfaceType = protocol.InterfaceType,
            ProtocolType = protocol.ProtocolType,
            IpAddress = protocol.IpAddress,
            ProtocolPort = protocol.ProtocolPort,
            SerialPortName = protocol.SerialPortName,
            BaudRate = protocol.BaudRate,
            DataBits = protocol.DataBits,
            Parity = protocol.Parity,
            StopBits = protocol.StopBits,
            CommunicationDelay = protocol.CommunicationDelay,
            ReceiveTimeOut = protocol.ReceiveTimeOut,
            ConnectTimeOut = protocol.ConnectTimeOut,
            Account = protocol.Account,
            Password = protocol.Password,
            Remark = protocol.Remark,
            Equipments = new List<EquipmentConfigDto>(protocol.Equipments)
        };
        Protocols.Add(duplicated);
    }

    #endregion

    #region 辅助方法

    partial void OnSelectedProtocolChanged(ProtocolConfigDto? value)
    {
        if (value != null)
        {
            UpdateAvailableProtocolTypes(value.InterfaceType);
        }
    }

    public void UpdateAvailableProtocolTypes(InterfaceType interfaceType)
    {
        // 根据接口类型过滤可用的协议类型
        AvailableProtocolTypes = interfaceType switch
        {
            InterfaceType.LAN => Enum.GetValues<ProtocolType>()
                .Where(p => (int)p < 100 || (int)p >= 200 && (int)p < 300)
                .ToList(),
            InterfaceType.COM => Enum.GetValues<ProtocolType>()
                .Where(p => (int)p >= 100 && (int)p < 200)
                .ToList(),
            InterfaceType.API => Enum.GetValues<ProtocolType>()
                .Where(p => (int)p >= 200 && (int)p < 300)
                .ToList(),
            InterfaceType.DATABASE => Enum.GetValues<ProtocolType>()
                .Where(p => (int)p >= 300)
                .ToList(),
            _ => new List<ProtocolType>()
        };
        OnPropertyChanged(nameof(AvailableProtocolTypes));
    }

    #endregion
}
