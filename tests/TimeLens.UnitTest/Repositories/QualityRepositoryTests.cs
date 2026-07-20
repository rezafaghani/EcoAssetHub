using System.Reflection;
using TimeLens.Infrastructure.Repositories;

namespace TimeLens.UnitTest.Repositories;

public class QualityRepositoryTests
{
    [Fact]
    public void BuildJobSelectSql_PutsWhereBeforeGroupBy()
    {
        var method = typeof(QualityRepository).GetMethod(
            "BuildJobSelectSql",
            BindingFlags.NonPublic | BindingFlags.Static);

        var sql = Assert.IsType<string>(method!.Invoke(null, ["j.enabled = true", true]));

        Assert.True(sql.IndexOf("WHERE j.enabled = true", StringComparison.Ordinal) < sql.IndexOf("GROUP BY j.id", StringComparison.Ordinal));
    }
}
