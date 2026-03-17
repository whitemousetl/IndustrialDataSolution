using IndustrialDataProcessor.Api.Models;
using IndustrialDataProcessor.Application.Features;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialDataProcessor.Api.Controllers;

/// <summary>
/// 工作站配置 API 控制器
/// </summary>
[ApiController]
[Route("api/workstation-config")]
[Produces("application/json")]
public class WorkstationConfigController(IMediator mediator, ILogger<WorkstationConfigController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<WorkstationConfigController> _logger = logger;

    /// <summary>
    /// 保存工作站配置
    /// </summary>
    /// <remarks>
    /// 此接口会同步等待数据库写入完成后立即返回响应。
    /// 后续的服务重启操作（连接重置、采集任务重启、OPC UA 服务器重启）将在后台异步执行。
    /// </remarks>
    /// <param name="dto">工作站配置数据</param>
    /// <param name="token">取消令牌</param>
    /// <returns>保存结果</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> SaveConfig([FromBody] WorkstationConfigDto dto, CancellationToken token = default)
    {
        var command = new SaveWorkstationConfigCommand(dto);

        // 同步等待数据库写入完成（包含验证、转换、存储）
        // 事件发布后，EventHandler 中的耗时操作会在后台异步执行
        // 验证失败 → ValidationBehavior 抛出 ValidationException → GlobalExceptionHandler 处理
        // 业务异常 → Handler 抛出 DomainException → GlobalExceptionHandler 处理
        // 基础设施异常 → Repository 抛出 InfrastructureException → GlobalExceptionHandler 处理
        await _mediator.Send(command, token);

        _logger.LogInformation("工作站配置保存成功，后台服务重启任务已触发。WorkstationId: {Id}", dto.Id);

        return Ok(ApiResponse.Ok("配置保存成功，服务正在后台更新"));
    }
}