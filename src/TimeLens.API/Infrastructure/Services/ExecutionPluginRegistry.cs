using TimeLens.Domain.Models;
using TimeLens.Domain.Services;

namespace TimeLens.API.Infrastructure.Services;

public class ExecutionPluginRegistry(IEnumerable<IExecutionPlugin> plugins)
{
    private readonly List<IExecutionPlugin> _plugins = plugins.OrderBy(x => x.Metadata.Id).ToList();

    public List<ExecutionPluginDto> List() => _plugins.Select(x => x.Metadata).ToList();

    public IExecutionPlugin? Resolve(string id) =>
        _plugins.SingleOrDefault(x => x.Metadata.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
