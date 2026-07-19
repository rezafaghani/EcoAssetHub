using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Domain.Models;
using EcoAssetHub.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using System.Xml;

namespace EcoAssetHub.API.Controllers;

[ApiController]
[Route("api/data-quality")]
public class DataQualityController(
    QualityValidatorCatalog validatorCatalog,
    IQualityRepository qualityRepository,
    IDatasetRepository datasetRepository,
    ITimeSeriesRepository timeSeriesRepository) : ControllerBase
{
    [HttpGet("validation-types")]
    [ProducesResponseType(typeof(List<QualityValidatorTypeDto>), StatusCodes.Status200OK)]
    public IActionResult ValidationTypes() => Ok(validatorCatalog.List());

    [HttpGet("groups")]
    [ProducesResponseType(typeof(List<QualityCurveGroupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Groups(CancellationToken cancellationToken)
    {
        return Ok(await qualityRepository.GetCurveGroupsAsync(cancellationToken));
    }

    [HttpPost("groups")]
    [ProducesResponseType(typeof(QualityCurveGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertGroup([FromBody] UpsertQualityCurveGroupRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("A group name is required.");
        }

        return Ok(await qualityRepository.UpsertCurveGroupAsync(request, cancellationToken));
    }

    [HttpPatch("groups/{id}/enabled")]
    [ProducesResponseType(typeof(QualityCurveGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetGroupEnabled([FromRoute] string id, [FromBody] SetEnabledRequest request, CancellationToken cancellationToken)
    {
        var group = await qualityRepository.SetCurveGroupEnabledAsync(id, request.Enabled, cancellationToken);
        return group is null ? NotFound() : Ok(group);
    }

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(List<QualityValidationJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Jobs(CancellationToken cancellationToken)
    {
        return Ok(await qualityRepository.GetJobsAsync(cancellationToken));
    }

    [HttpGet("jobs/{id}")]
    [ProducesResponseType(typeof(QualityValidationJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Job([FromRoute] string id, CancellationToken cancellationToken)
    {
        var job = await qualityRepository.GetJobAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("jobs")]
    [ProducesResponseType(typeof(QualityValidationJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertJob([FromBody] UpsertQualityValidationJobRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.CronExpression)
            || string.IsNullOrWhiteSpace(request.WindowStartExpression)
            || string.IsNullOrWhiteSpace(request.WindowEndExpression)
            || request.Targets.Count == 0
            || request.Checks.Count == 0)
        {
            return BadRequest("A quality job requires a name, schedule, evaluation window, target, and validation check.");
        }

        var validatorIds = validatorCatalog.List().Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (request.Checks.Any(x => !validatorIds.Contains(x.ValidatorId)))
        {
            return BadRequest("One or more validation checks are not supported.");
        }

        return Ok(await qualityRepository.UpsertJobAsync(request, cancellationToken));
    }

    [HttpPatch("jobs/{id}/enabled")]
    [ProducesResponseType(typeof(QualityValidationJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetJobEnabled([FromRoute] string id, [FromBody] SetEnabledRequest request, CancellationToken cancellationToken)
    {
        var job = await qualityRepository.SetJobEnabledAsync(id, request.Enabled, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("findings")]
    [ProducesResponseType(typeof(List<QualityFindingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Findings(
        [FromQuery] string? datasetId,
        [FromQuery] string? curveId,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        return Ok(await qualityRepository.GetFindingsAsync(datasetId, curveId, activeOnly, cancellationToken));
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(QualityStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status(
        [FromQuery] string? datasetId,
        [FromQuery] string? curveId,
        CancellationToken cancellationToken)
    {
        var status = await qualityRepository.GetStatusAsync(datasetId, curveId, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(QualitySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        return Ok(await qualityRepository.GetSummaryAsync(cancellationToken));
    }

    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(ManualQualityEvaluationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Evaluate([FromBody] ManualQualityEvaluationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DatasetId)
            || !DateTimeExpression.TryResolve(request.Start, out var start, request.TimeZone)
            || !DateTimeExpression.TryResolve(request.End, out var end, request.TimeZone)
            || start >= end)
        {
            return BadRequest("A valid dataset id and half-open date range are required.");
        }

        DateTimeOffset? asOf = null;
        if (!string.IsNullOrWhiteSpace(request.AsOf))
        {
            if (!DateTimeExpression.TryResolve(request.AsOf, out var parsedAsOf, request.TimeZone))
            {
                return BadRequest("A valid asOf version time is required.");
            }

            asOf = parsedAsOf;
        }

        var metadata = await datasetRepository.GetAsync(request.DatasetId, cancellationToken);
        if (metadata is null)
        {
            return NotFound();
        }

        if (!TryParseDuration(request.Granularity ?? metadata.Granularity, out var granularity)
            || !TryParseOptionalDuration(request.AllowedDelay, out var allowedDelay))
        {
            return BadRequest("Granularity and allowedDelay must be ISO-8601 durations, for example PT15M.");
        }

        var points = await timeSeriesRepository.GetSeriesAsync(request.DatasetId, start, end, asOf, cancellationToken);
        var findings = QualityValidationEngine.Evaluate(new QualityEvaluationRequest(
            metadata,
            start,
            end,
            DateTimeOffset.UtcNow,
            granularity,
            allowedDelay,
            request.MinimumValue,
            request.MaximumValue,
            request.MaximumAbsoluteChange,
            request.MaximumPercentageChange,
            request.NearZeroFloor ?? 0.000001,
            request.FlatLinePointCount ?? 0,
            points));

        var result = new ManualQualityEvaluationResult(metadata, start, end, points.Count, OverallStatus(findings), findings);
        var executionId = await qualityRepository.SaveManualEvaluationAsync(result, cancellationToken);
        return Ok(result with { ExecutionId = executionId });
    }

    private static string OverallStatus(List<QualityFindingDraftDto> findings)
    {
        if (findings.Any(x => x.QualityStatus == QualityStatuses.Critical))
        {
            return QualityStatuses.Critical;
        }

        return findings.Count == 0 ? QualityStatuses.Healthy : QualityStatuses.Degraded;
    }

    private static bool TryParseOptionalDuration(string? value, out TimeSpan? duration)
    {
        duration = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!TryParseDuration(value, out var parsed))
        {
            return false;
        }

        duration = parsed;
        return true;
    }

    private static bool TryParseDuration(string value, out TimeSpan duration)
    {
        try
        {
            duration = XmlConvert.ToTimeSpan(value);
            return duration > TimeSpan.Zero;
        }
        catch (FormatException)
        {
            duration = default;
            return false;
        }
    }
}

public record SetEnabledRequest(bool Enabled);
