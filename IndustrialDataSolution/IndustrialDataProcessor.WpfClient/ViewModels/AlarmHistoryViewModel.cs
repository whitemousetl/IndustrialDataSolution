using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class AlarmHistoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pageTitle = "历史报警";
}
