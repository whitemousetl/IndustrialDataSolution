using IndustrialDataProcessor.Application.Commands;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialDataProcessor.Api.Controllers;

[ApiController]
[Route("api/workstation-config")]
public class WorkstationConfigController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    [HttpPost]
    public async Task<IActionResult> SaveConfing([FromBody] WorkstationConfigDto dto, CancellationToken token = default)
    {
        // 将 HTTP 请求转化为领域命令，发送给 MediatR
        var command = new SaveWorkstationConfigCommand(dto);
        await _mediator.Send(command, token);
        return Ok(new { success = true, message = "配置保存成功" });
    }
}