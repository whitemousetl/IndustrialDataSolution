using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Controls;
using IndustrialDataProcessor.WpfClient.Interfaces;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class WorkstationConfigViewModel : ObservableObject, IConfigSaveable
{
    [ObservableProperty]
    private string _pageTitle = "工作站配置管理";

    public void SaveConfig()
    {
        Growl.Success("工作站配置已保存");
    }
}
