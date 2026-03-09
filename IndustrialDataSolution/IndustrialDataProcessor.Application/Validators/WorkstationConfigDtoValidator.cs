using FluentValidation;
using IndustrialDataProcessor.Application.Dtos.WorkstationDto;

namespace IndustrialDataProcessor.Application.Validators;

public class WorkstationConfigDtoValidator : AbstractValidator<WorkstationConfigDto>
{
    public WorkstationConfigDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("工作站ID不能为空");

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .WithMessage("工作站IP地址不能为空")
            .Must(BeValidIpAddress)
            .WithMessage("IP地址格式不正确");

        RuleFor(x => x.Protocols)
            .NotEmpty()
            .WithMessage("协议列表不能为空")
            .ForEach(protocol => protocol.SetValidator(new ProtocolConfigDtoValidator()));
    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        var result = System.Net.IPAddress.TryParse(ipAddress, out var parsedIp)
               && parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        return result;
    }
}
