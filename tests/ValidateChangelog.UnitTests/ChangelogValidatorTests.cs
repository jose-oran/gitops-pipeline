using System.Reflection;
using ValidateChangelog;

namespace ValidateChangelog.UnitTests;

public class ChangelogValidatorTests
{
    [Fact]
    public void ValidateFile_Fails_When_The_File_Does_Not_Exist()
    {
        var result = ChangelogValidator.ValidateFile("does-not-exist.md", "1.0.0");

        Assert.False(result.IsValid);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public void Validate_Fails_When_The_Version_Heading_Is_Missing()
    {
        var lines = new[] { "# Changelog", "", "## [0.9.0]", "- something" };

        var result = ChangelogValidator.Validate(lines, "1.0.0");

        Assert.False(result.IsValid);
        Assert.Contains("No '## [1.0.0]'", result.Message);
    }

    [Fact]
    public void Validate_Fails_When_The_Heading_Has_No_Content_Before_The_Next_Heading()
    {
        var lines = new[] { "# Changelog", "", "## [1.0.0]", "", "## [0.9.0]", "- old entry" };

        var result = ChangelogValidator.Validate(lines, "1.0.0");

        Assert.False(result.IsValid);
        Assert.Contains("no content", result.Message);
    }

    [Fact]
    public void Validate_Fails_When_The_Heading_Is_The_Last_Line_With_No_Content_After_It()
    {
        var lines = new[] { "# Changelog", "", "## [1.0.0]" };

        var result = ChangelogValidator.Validate(lines, "1.0.0");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Succeeds_When_The_Heading_Has_At_Least_One_Content_Line()
    {
        var lines = new[] { "# Changelog", "", "## [1.0.0]", "- fixed the thing", "", "## [0.9.0]" };

        var result = ChangelogValidator.Validate(lines, "1.0.0");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateFile_Succeeds_Against_This_Repos_Own_CHANGELOG()
    {
        var repoRoot = FindRepoRoot();
        var changelogPath = Path.Combine(repoRoot, "CHANGELOG.md");

        var result = ChangelogValidator.ValidateFile(changelogPath, "1.0.0");

        Assert.True(result.IsValid);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CHANGELOG.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repo root (CHANGELOG.md not found in any parent directory)");
    }
}
