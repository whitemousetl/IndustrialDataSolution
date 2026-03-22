using IndustrialDataProcessor.WpfClient.ViewModels;

namespace IndustrialDataProcessor.WpfClient.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // 简单方式：直接创建 ViewModel 并赋值 DataContext
            // 生产环境建议通过依赖注入/IOC 提供 ViewModel
            DataContext = new MainWindowViewModel();
        }
    }
}