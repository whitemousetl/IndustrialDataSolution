using System.ComponentModel;

namespace IndustrialDataProcessor.Domain.Enums;

/// <summary>
/// 请求方式
/// </summary>
public enum RequestMethod
{
    /// <summary>
    /// Get
    /// </summary>
    [Description("Get")]
    Get = 0,

    /// <summary>
    /// Post
    /// </summary>
    [Description("Post")]
    Post = 1,

    /// <summary>
    /// Put
    /// </summary>
    [Description("Put")]
    Put = 2,

    /// <summary>
    /// Delete
    /// </summary>
    [Description("Delete")]
    Delete = 3,

    /// <summary>
    /// Patch
    /// </summary>
    [Description("Patch")]
    Patch = 4,
}
