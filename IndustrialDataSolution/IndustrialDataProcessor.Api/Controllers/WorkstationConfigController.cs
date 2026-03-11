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
        try
        {
            var command = new SaveWorkstationConfigCommand(dto);
            
            // 同步等待数据库写入完成（包含验证、转换、存储）
            // 事件发布后，EventHandler 中的耗时操作会在后台异步执行
            await _mediator.Send(command, token);
            
            _logger.LogInformation("工作站配置保存成功，后台服务重启任务已触发。WorkstationId: {Id}", dto.Id);
            
            return Ok(ApiResponse.Ok("配置保存成功，服务正在后台更新"));
        }
        catch (FluentValidation.ValidationException ex)
        {
            // 验证失败
            var errors = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("工作站配置验证失败: {Errors}", errors);
            return BadRequest(ApiResponse.Fail($"配置验证失败: {errors}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存工作站配置时发生错误");
            return StatusCode(500, ApiResponse.Error("保存配置失败，请稍后重试"));
        }
    }
}