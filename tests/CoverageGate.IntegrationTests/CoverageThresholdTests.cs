using System.Diagnostics;

namespace CoverageGate.IntegrationTests;

// Proves the coverage gate actually enforces a threshold, not just reports one.
// Runs `dotnet test` for real against two twin fixtures with the same 80% threshold:
// one well-tested (must pass), one deliberately under-tested (must fail).
public class CoverageThresholdTests
{
    [Fact]
    public void HighCoverageSample_Passes_The_Threshold()
    {
        var exitCode = RunCoverageGate("HighCoverageSample");

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void LowCoverageSample_Fails_The_Threshold()
    {
        var exitCode = RunCoverageGate("LowCoverageSample");

        Assert.NotEqual(0, exitCode);
    }

    private static int RunCoverageGate(string fixtureName)
    {
        var fixtureDir = Path.Combine(FindRepoRoot(), "tests", "CoverageGate.IntegrationTests", "fixtures", fixtureName);
        var testsDir = Path.Combine(fixtureDir, "Tests");

        CleanBuildArtifacts(Path.Combine(fixtureDir, "Lib"));
        CleanBuildArtifacts(testsDir);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "test -p:CollectCoverage=true -p:Threshold=80 -p:ThresholdType=line -p:ThresholdStat=total --nologo",
            WorkingDirectory = testsDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start dotnet test for '{fixtureName}'");

        process.WaitForExit();

        return process.ExitCode;
    }

    private static void CleanBuildArtifacts(string projectDir)
    {
        foreach (var dirName in new[] { "bin", "obj" })
        {
            var path = Path.Combine(projectDir, dirName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "GitOpsPipeline.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate GitOpsPipeline.slnx above " + AppContext.BaseDirectory);
    }
}
