using FluentValidation;

namespace InspectionService.Application.Inspections.Commands.UpdateInspection;

/// <summary>
/// Validator for UpdateInspectionCommand
/// </summary>
public class UpdateInspectionCommandValidator : AbstractValidator<UpdateInspectionCommand>
{
    public UpdateInspectionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Inspection ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");
    }
}
