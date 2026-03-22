using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class TaskStatusViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pageTitle = "运行状态";
}
