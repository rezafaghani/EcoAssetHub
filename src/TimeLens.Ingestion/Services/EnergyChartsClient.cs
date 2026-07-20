using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Net;

namespace TimeLens.Ingestion.Services;

public class EnergyChartsClient(
    HttpClient httpClient,
    EnergyChartsRateLimiter rateLimiter,
    IOptions<IngestionOptions> options,
    ILogger<EnergyChartsClient> logger)
{
    public async Task<JsonDocument> GetAsync(EnergyChartsDatasetDefinition definition, CancellationToken cancellationToken)
    {
        var uri = definition.Endpoint.TrimStart('/') + EnergyChartsDefaults.ToQueryString(definition.Parameters);
        var maxRetries = Math.Max(options.Value.MaxEnergyChartsRetries, 0);

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            await rateLimiter.WaitAsync(cancellationToken);

            using var response = await httpClient.GetAsync(uri, cancellationToken);
            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt == maxRetries)
            {
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }

            var delay = GetRetryDelay(response, attempt);
            logger.LogWarning(
                "Energy Charts returned 429 for {Endpoint}. Retrying in {DelaySeconds:n0}s ({Attempt}/{MaxAttempts}).",
                definition.Endpoint,
                delay.TotalSeconds,
                attempt + 1,
                maxRetries);
            await Task.Delay(delay, cancellationToken);
        }

        throw new InvalidOperationException("Energy Charts request retry loop exited unexpectedly.");
    }

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } retryAfterDelta)
        {
            return retryAfterDelta;
        }

        if (response.Headers.RetryAfter?.Date is { } retryAfterDate)
        {
            var delay = retryAfterDate - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        var baseDelaySeconds = Math.Max(options.Value.RetryBaseDelaySeconds, 1);
        return TimeSpan.FromSeconds(baseDelaySeconds * Math.Pow(2, attempt));
    }
}
