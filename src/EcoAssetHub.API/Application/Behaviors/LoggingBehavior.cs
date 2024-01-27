namespace EcoAssetHub.API.Application.Behaviors; // Namespace declaration

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) // Class declaration with generic type parameters and constructor injection
    : IPipelineBehavior<TRequest, TResponse> // Class implements IPipelineBehavior interface with generic type parameters
    where TRequest : IRequest<TResponse> // Constraint on TRequest type parameter
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) // Method declaration with parameters
    {
        logger.LogInformation("Handling command {CommandName} ({@Command})", request.GetGenericTypeName(), request); // Logging information using logger instance
        var response = await next(); // Call the next handler in the pipeline
        logger.LogInformation("Command {CommandName} handled - response: {@Response}", request.GetGenericTypeName(), response); // Logging information using logger instance

        return response; // Return the response
    }
}