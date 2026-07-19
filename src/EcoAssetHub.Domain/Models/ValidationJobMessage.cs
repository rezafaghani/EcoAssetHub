using System.Text.Json;

namespace EcoAssetHub.Domain.Models;

public class ValidationJobMessage
{
    public string JobId { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public string TriggerType { get; set; } = "scheduled";
    public string WindowStartExpression { get; set; } = string.Empty;
    public string WindowEndExpression { get; set; } = string.Empty;
}

public class ValidationCheckMessage
{
    public string JobId { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public string ValidatorId { get; set; } = string.Empty;
    public int ValidatorVersion { get; set; } = 1;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public JsonElement Configuration { get; set; }
}
