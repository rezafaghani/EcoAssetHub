using System.Reflection;
using TimeLens.Infrastructure.Repositories;

namespace TimeLens.UnitTest.Repositories;

public class ExecutionRepositoryTests
{
    [Fact]
    public void BuildDefinitionSelectSql_PutsWhereBeforeGroupBy()
    {
        var method = typeof(ExecutionRepository).GetMethod(
            "BuildDefinitionSelectSql",
            BindingFlags.NonPublic | BindingFlags.Static);

        var sql = Assert.IsType<string>(method!.Invoke(null, ["d.enabled = true", true]));

        Assert.True(sql.IndexOf("WHERE d.enabled = true", StringComparison.Ordinal) < sql.IndexOf("GROUP BY d.id", StringComparison.Ordinal));
    }
}
