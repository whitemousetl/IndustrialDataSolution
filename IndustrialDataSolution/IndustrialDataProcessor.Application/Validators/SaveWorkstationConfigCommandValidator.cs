using FluentValidation;
using IndustrialDataProcessor.Application.Features;

namespace IndustrialDataProcessor.Application.Validators;

public class SaveWorkstationConfigCommandValidator : AbstractValidator<SaveWorkstationConfigCommand>
{
	public SaveWorkstationConfigCommandValidator()
	{
		RuleFor(x => x.Dto).SetValidator(new WorkstationConfigDtoValidator());
	}
}
