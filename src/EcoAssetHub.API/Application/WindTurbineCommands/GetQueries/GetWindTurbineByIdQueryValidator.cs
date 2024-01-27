namespace EcoAssetHub.API.Application.WindTurbineCommands.GetQueries;

public class GetWindTurbineByIdQueryValidator: AbstractValidator<GetWindTurbineByIdQuery>
{
    public GetWindTurbineByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithMessage("Id is required.");
        // Additional validation rules as needed
    }
}