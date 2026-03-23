namespace IndustrialDataProcessor.WpfClient.Interfaces;

/// <summary>
/// 配置保存接口，供子页面 ViewModel 实现
/// </summary>
public interface IConfigSaveable
{
    Task SaveConfig();
}
