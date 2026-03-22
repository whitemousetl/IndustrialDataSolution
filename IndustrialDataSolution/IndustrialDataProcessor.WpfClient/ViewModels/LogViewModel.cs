using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialDataProcessor.WpfClient.ViewModels;

public partial class LogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pageTitle = "日志管理";
}

