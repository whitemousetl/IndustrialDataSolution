using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class TaskControlViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pageTitle = "任务下发管理";
}
