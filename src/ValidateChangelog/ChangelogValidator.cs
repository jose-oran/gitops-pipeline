namespace ValidateChangelog;

public sealed record ValidationResult(bool IsValid, string Message);

/// <summary>
/// Mirrors the real <c>validate_changelog.sh</c> gate: a version is only considered
/// documented if its <c>## [version]</c> heading exists <em>and</em> has at least one
/// non-empty content line before the next heading (or end of file) - a bare heading with no
/// bullets still fails, exactly like the shell script this ports.
/// </summary>
public static class ChangelogValidator
{
    public static ValidationResult ValidateFile(string changelogPath, string version)
    {
        if (!File.Exists(changelogPath))
        {
            return new ValidationResult(false, $"CHANGELOG not found at '{changelogPath}'");
        }

        return Validate(File.ReadAllLines(changelogPath), version);
    }

    public static ValidationResult Validate(IReadOnlyList<string> lines, string version)
    {
        var headingIndex = FindHeadingIndex(lines, version);
        if (headingIndex is null)
        {
            return new ValidationResult(false, $"No '## [{version}]' entry found in CHANGELOG");
        }

        for (var i = headingIndex.Value + 1; i < lines.Count; i++)
        {
            if (IsHeading(lines[i]))
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                return new ValidationResult(true, $"CHANGELOG entry for {version} is valid");
            }
        }

        return new ValidationResult(false, $"'## [{version}]' entry has no content");
    }

    private static int? FindHeadingIndex(IReadOnlyList<string> lines, string version)
    {
        var expected = $"## [{version}]";
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].TrimStart().StartsWith(expected, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return null;
    }

    private static bool IsHeading(string line) => line.TrimStart().StartsWith("## [", StringComparison.Ordinal);
}
