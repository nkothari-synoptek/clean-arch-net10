using FluentValidation;

namespace InspectionService.Application.Inspections.Commands.CreateInspection;

/// <summary>
/// Validator for CreateInspectionCommand
/// </summary>
public class CreateInspectionCommandValidator : AbstractValidator<CreateInspectionCommand>
{
    public CreateInspectionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required")
            .MaximumLength(100).WithMessage("CreatedBy must not exceed 100 characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one inspection item is required");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Item name is required")
                .MaximumLength(200).WithMessage("Item name must not exceed 200 characters");

            item.RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Item description is required")
                .MaximumLength(1000).WithMessage("Item description must not exceed 1000 characters");

            item.RuleFor(x => x.Order)
                .GreaterThan(0).WithMessage("Item order must be greater than 0");
        });
    }
}
