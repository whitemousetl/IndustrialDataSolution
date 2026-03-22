using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class AlarmRealtimeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pageTitle = "实时监控";
}