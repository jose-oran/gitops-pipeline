using System.Text.RegularExpressions;

namespace BumpImageTag;

/// <summary>
/// Closes a real gap found in the workspace this mirrors: no Jenkinsfile ever updates the CD
/// repo's image tag or pushes to it - deployment is fully decoupled from CI, and someone
/// bumps `values.yml` by hand. This does it as a surgical text replacement (not a full
/// YAML parse+reserialize) specifically so the rest of the file - comments, key order,
/// formatting - is left completely untouched.
/// </summary>
public static class ImageTagBumper
{
    public static string BumpTag(string kustomizationContent, string imageName, string newTag)
    {
        var pattern = new Regex(
            $@"(-\s*name:\s*{Regex.Escape(imageName)}\s*\r?\n\s*newTag:\s*)(\S+)",
            RegexOptions.Multiline);

        if (!pattern.IsMatch(kustomizationContent))
        {
            throw new InvalidOperationException($"No image entry named '{imageName}' with a newTag field was found");
        }

        return pattern.Replace(kustomizationContent, match => match.Groups[1].Value + newTag);
    }

    public static void BumpTagInFile(string kustomizationPath, string imageName, string newTag)
    {
        var content = File.ReadAllText(kustomizationPath);
        var updated = BumpTag(content, imageName, newTag);
        File.WriteAllText(kustomizationPath, updated);
    }
}
