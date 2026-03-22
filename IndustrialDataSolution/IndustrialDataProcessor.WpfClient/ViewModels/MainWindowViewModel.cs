using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using HandyControl.Data;
using IndustrialDataProcessor.WpfClient.Interfaces;
using System.Windows.Threading;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    // ==================== 当前页面 ====================
    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _currentViewName = "工作站配置";

    // ==================== 连接状态 ====================
    [ObservableProperty]
    private bool _isOpcUaConnected;

    [ObservableProperty]
    private string _opcUaStatusText = "未连接";

    [ObservableProperty]
    private bool _isApiConnected;

    [ObservableProperty]
    private string _apiStatusText = "未连接";

    [ObservableProperty]
    private string _currentUserName = "管理员";

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    private readonly DispatcherTimer _timer;

    public MainWindowViewModel()
    {
        // 默认显示工作站配置页
        NavigateTo("WorkstationConfig");

        // 时间刷新定时器
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        _timer.Tick += (_, _) =>
        {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        };

        _timer.Start();
    }

    [RelayCommand]
    private void Navigate(string? parameter)
    {
        if (string.IsNullOrEmpty(parameter))
            return;

        NavigateTo(parameter);
    }

    private void NavigateTo(string key)
    {
        // 明确指定元组中第一个元素为 ObservableObject?，避免类型推断失败
        var result = key switch
        {
            "WorkstationConfig" => ((ObservableObject?)new WorkstationConfigViewModel(), "工作站配置"),
            "ProtocolConfig" => ((ObservableObject?)new ProtocolConfigViewModel(), "协议配置"),
            "TaskControl" => ((ObservableObject?)new TaskControlViewModel(), "任务下发"),
            "TaskStatus" => ((ObservableObject?)new TaskStatusViewModel(), "运行状态"),
            "RealTimeMonitor" => ((ObservableObject?)new RealTimeMonitorViewModel(), "实时监控"),
            "AlarmRealtime" => ((ObservableObject?)new AlarmRealtimeViewModel(), "事实报警"),
            "AlarmHistory" => ((ObservableObject?)new AlarmHistoryViewModel(), "历史报警"),
            "Log" => ((ObservableObject?)new LogViewModel(), "日志"),
            "Settings" => ((ObservableObject?)new SettingsViewModel(), "设置"),
            _ => ((ObservableObject?)null, "未知页面")
        };

        // 分别赋值到 Observable 属性
        CurrentViewModel = result.Item1;
        CurrentViewName = result.Item2;
    }

    /// <summary>
    /// SideMenu SelectionChanged 事件绑定
    /// </summary>
    /// <param name="args"></param>
    [RelayCommand]
    private void NavigationChanged(FunctionEventArgs<object> args)
    {
        if (args.Info is SideMenuItem menuItem)
        {
            // 只有叶子节点 (有 CommandParameter 的) 才导航
            var hander = menuItem.Header?.ToString();
            if (!string.IsNullOrEmpty(hander))
            {
                // 根据菜单 Header 映射到导航key
                var key = hander switch
                {
                    "工作站配置" => "WorkstationConfig",
                    "协议配置" => "ProtocolConfig",
                    "任务下发" => "TaskControl",
                    "运行状态" => "TaskStatus",
                    "实时监控" => "RealTimeMonitor",
                    "事实报警" => "AlarmRealtime",
                    "历史报警" => "AlarmHistory",
                    "日志" => "Log",
                    "设置" => "Settings",
                    _ => null
                };

                if (!string.IsNullOrEmpty(key))
                    NavigateTo(key);
            }
        }
    }

    // ==================== 工具栏命令 ====================
    [RelayCommand]
    private void SaveCurrentConfig()
    {
        // 转发给当前页面的 ViewModel 来保存配置
        if (CurrentViewModel is IConfigSaveable saveable)
            saveable.SaveConfig();
    }

    [RelayCommand]
    private void ConnectionOpcUa()
    {
        // 调用 OPC UA 客户端服务

        // 成功后更新状态
        IsOpcUaConnected = true;
        OpcUaStatusText = "已连接";
        Growl.Success("OPC UA 连接成功");
    }

    [RelayCommand]
    private void ConnectApi()
    {
        // 调用Api 连接服务
        IsApiConnected = true;
        ApiStatusText = "已连接";
        Growl.Success("API 连接成功");
    }

    [RelayCommand]
    private void OpenSettings()
    {
        NavigateTo("Settings");
    }

    [RelayCommand]
    private void OpenProfile()
    {
        Growl.Info("个人设置功能待实现");
    }

    [RelayCommand]
    private void Logout()
    {
        Growl.Info("退出登录功能待实现");
    }

}



