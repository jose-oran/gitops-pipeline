using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ComputeSyncOrder;

/// <summary>
/// Computes the order ArgoCD would actually apply resources in, from real
/// <c>argocd.argoproj.io/sync-wave</c> annotations - a pattern that doesn't exist anywhere
/// in the production workspace this mirrors, despite being claimed. Order: ascending by
/// wave (resources without an annotation default to wave 0, matching ArgoCD's own default),
/// and within the same wave, the order Kustomize's own `resources:` list and each file's
/// document order would produce.
/// </summary>
public static class SyncOrderCalculator
{
    private const string SyncWaveAnnotation = "argocd.argoproj.io/sync-wave";
    private static readonly Regex DocumentSeparator = new(@"(?m)^---\s*$", RegexOptions.Compiled);

    public static IReadOnlyList<ResourceIdentity> ComputeOrder(string kustomizationDir)
    {
        var kustomizationPath = ResolveKustomizationPath(kustomizationDir);
        var resourceFiles = ReadResourceFileList(kustomizationPath);

        var resources = new List<ResourceIdentity>();
        foreach (var fileName in resourceFiles)
        {
            resources.AddRange(ParseResources(Path.Combine(kustomizationDir, fileName)));
        }

        // List.Sort/OrderBy in .NET is a stable sort, so resources within the same wave keep
        // the relative order they were discovered in - the same guarantee ArgoCD itself gives.
        return resources.OrderBy(resource => resource.SyncWave).ToList();
    }

    private static string ResolveKustomizationPath(string kustomizationDir)
    {
        foreach (var candidate in new[] { "kustomization.yml", "kustomization.yaml" })
        {
            var path = Path.Combine(kustomizationDir, candidate);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException($"No kustomization.yml(aml) found under '{kustomizationDir}'");
    }

    private static List<string> ReadResourceFileList(string kustomizationPath)
    {
        var deserializer = BuildDeserializer();
        var kustomization = deserializer.Deserialize<Kustomization>(File.ReadAllText(kustomizationPath));
        return kustomization.Resources;
    }

    private static IEnumerable<ResourceIdentity> ParseResources(string filePath)
    {
        var deserializer = BuildDeserializer();
        var content = File.ReadAllText(filePath).Replace("\r\n", "\n");

        foreach (var document in DocumentSeparator.Split(content))
        {
            if (string.IsNullOrWhiteSpace(document))
            {
                continue;
            }

            var resource = deserializer.Deserialize<K8sResource>(document);
            if (resource is null || string.IsNullOrEmpty(resource.Kind))
            {
                continue;
            }

            var wave = 0;
            if (resource.Metadata.Annotations?.TryGetValue(SyncWaveAnnotation, out var waveText) == true)
            {
                wave = int.Parse(waveText);
            }

            yield return new ResourceIdentity(resource.Kind, resource.Metadata.Name, wave);
        }
    }

    private static IDeserializer BuildDeserializer() =>
        new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
}
