using FluentValidation;

namespace EcoAssetHub.API.Application.SolarPanelCommands.GetQueries;

public class GetSolarPanelByIdQueryValidator : AbstractValidator<GetSolarPanelByIdQuery>
{
    public GetSolarPanelByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithMessage("Id is required.");
        // Additional validation rules as needed
    }
}