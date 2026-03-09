using FluentValidation;
using IndustrialDataProcessor.Application.Commands;

namespace IndustrialDataProcessor.Application.Validators;

public class SaveWorkstationConfigCommandValidator : AbstractValidator<SaveWorkstationConfigCommand>
{
	public SaveWorkstationConfigCommandValidator()
	{
		RuleFor(x => x.dto).SetValidator(new WorkstationConfigDtoValidator());
	}
}
