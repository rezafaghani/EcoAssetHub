using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers;

[ApiController]
public class ExecutionController(
    ExecutionPluginRegistry pluginRegistry,
    ExecutionRuntime executionRuntime,
    IExecutionRepository executionRepository) : ControllerBase
{
    [HttpGet("api/plugins")]
    [ProducesResponseType(typeof(List<ExecutionPluginDto>), StatusCodes.Status200OK)]
    public IActionResult Plugins([FromQuery] string? category)
    {
        var plugins = pluginRegistry.List();
        if (!string.IsNullOrWhiteSpace(category))
        {
            plugins = plugins.Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Ok(plugins);
    }

    [HttpGet("api/plugins/{id}")]
    [ProducesResponseType(typeof(ExecutionPluginDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Plugin([FromRoute] string id)
    {
        var plugin = pluginRegistry.List().SingleOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return plugin is null ? NotFound() : Ok(plugin);
    }

    [HttpGet("api/execution-definitions")]
    [ProducesResponseType(typeof(List<ExecutionDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Definitions(CancellationToken cancellationToken)
    {
        return Ok(await executionRepository.GetDefinitionsAsync(cancellationToken));
    }

    [HttpGet("api/execution-definitions/{id}")]
    [ProducesResponseType(typeof(ExecutionDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Definition([FromRoute] string id, CancellationToken cancellationToken)
    {
        var definition = await executionRepository.GetDefinitionAsync(id, cancellationToken);
        return definition is null ? NotFound() : Ok(definition);
    }

    [HttpPost("api/execution-definitions")]
    [ProducesResponseType(typeof(ExecutionDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertDefinition([FromBody] UpsertExecutionDefinitionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.CronExpression)
            || string.IsNullOrWhiteSpace(request.WindowStartExpression)
            || string.IsNullOrWhiteSpace(request.WindowEndExpression)
            || request.Targets.Count == 0
            || request.Plugins.Count == 0)
        {
            return BadRequest("An execution definition requires a name, schedule, evaluation window, target, and plugin.");
        }

        var pluginIds = pluginRegistry.List().Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (request.Plugins.Any(x => !pluginIds.Contains(x.PluginId)))
        {
            return BadRequest("One or more plugins are not available in this environment.");
        }

        return Ok(await executionRepository.UpsertDefinitionAsync(request, cancellationToken));
    }

    [HttpPatch("api/execution-definitions/{id}/enabled")]
    [ProducesResponseType(typeof(ExecutionDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefinitionEnabled([FromRoute] string id, [FromBody] SetEnabledRequest request, CancellationToken cancellationToken)
    {
        var definition = await executionRepository.SetDefinitionEnabledAsync(id, request.Enabled, cancellationToken);
        return definition is null ? NotFound() : Ok(definition);
    }

    [HttpPost("api/execution-definitions/{id}/runs")]
    [ProducesResponseType(typeof(RunExecutionDefinitionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunDefinition([FromRoute] string id, [FromBody] RunExecutionDefinitionRequest request, CancellationToken cancellationToken)
    {
        var definition = await executionRepository.GetDefinitionAsync(id, cancellationToken);
        if (definition is null)
        {
            return NotFound();
        }

        if (!DateTimeExpression.TryResolve(request.Start ?? definition.WindowStartExpression, out var start, definition.TimeZone)
            || !DateTimeExpression.TryResolve(request.End ?? definition.WindowEndExpression, out var end, definition.TimeZone)
            || start >= end)
        {
            return BadRequest("A valid half-open evaluation window is required.");
        }

        try
        {
            var results = await executionRuntime.ExecuteAsync(definition, start, end, cancellationToken);
            var run = await executionRepository.SaveRunAsync(
                definition.Id,
                string.IsNullOrWhiteSpace(request.TriggerType) ? "manual" : request.TriggerType,
                start,
                end,
                results,
                cancellationToken);
            return Ok(new RunExecutionDefinitionResult(run, results));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("api/executions")]
    [ProducesResponseType(typeof(List<ExecutionRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Runs([FromQuery] string? definitionId, CancellationToken cancellationToken)
    {
        return Ok(await executionRepository.GetRunsAsync(definitionId, cancellationToken));
    }
}
