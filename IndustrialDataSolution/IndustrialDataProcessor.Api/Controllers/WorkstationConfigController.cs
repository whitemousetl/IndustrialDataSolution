using IndustrialDataProcessor.Application.Commands;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialDataProcessor.Api.Controllers;

/// <summary>
/// 工作站配置 API 控制器
/// </summary>
[ApiController]
[Route("api/workstation-config")]
public class WorkstationConfigController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    /// <summary>
    /// 保存工作站配置
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveConfig([FromBody] WorkstationConfigDto dto, CancellationToken token = default)
    {
        var command = new SaveWorkstationConfigCommand(dto);
        await _mediator.Send(command, token);
        return Ok(new { success = true, message = "配置保存成功" });
    }
}