using CommunityToolkit.Mvvm.ComponentModel;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class RealTimeMonitorViewModel : ObservableObject
{
  [ObservableProperty]
    private string _pageTitle = "实时监控";
}
