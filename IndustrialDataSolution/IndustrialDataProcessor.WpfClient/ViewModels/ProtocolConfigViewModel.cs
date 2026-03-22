using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class ProtocolConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pageTitle = "协议配置管理";
}
