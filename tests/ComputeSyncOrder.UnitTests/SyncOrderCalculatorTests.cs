using System.Reflection;
using ComputeSyncOrder;

namespace ComputeSyncOrder.UnitTests;

public class SyncOrderCalculatorTests
{
    [Fact]
    public void ComputeOrder_Orders_The_Real_Cd_Manifests_By_Sync_Wave()
    {
        var cdDevDir = Path.Combine(FindRepoRoot(), "cd", "dev");

        var order = SyncOrderCalculator.ComputeOrder(cdDevDir);

        Assert.Equal(
            [
                "Namespace/gitops-pipeline-dev",
                "ConfigMap/app-config",
                "Deployment/redis",
                "Service/redis",
                "Deployment/cache-api",
                "Service/cache-api",
            ],
            order.Select(resource => resource.ToString()));

        Assert.Equal(0, order[0].SyncWave);
        Assert.Equal(1, order[2].SyncWave);
        Assert.Equal(2, order[4].SyncWave);
    }

    [Fact]
    public void ComputeOrder_Throws_A_Clear_Error_When_No_Kustomization_Exists()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), $"sync-order-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyDir);

        try
        {
            Assert.Throws<FileNotFoundException>(() => SyncOrderCalculator.ComputeOrder(emptyDir));
        }
        finally
        {
            Directory.Delete(emptyDir, recursive: true);
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CHANGELOG.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repo root");
    }
}
