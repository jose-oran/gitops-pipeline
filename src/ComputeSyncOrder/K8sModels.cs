namespace ComputeSyncOrder;

internal sealed class K8sResource
{
    public string Kind { get; set; } = "";
    public K8sMetadata Metadata { get; set; } = new();
}

internal sealed class K8sMetadata
{
    public string Name { get; set; } = "";
    public Dictionary<string, string>? Annotations { get; set; }
}

internal sealed class Kustomization
{
    public List<string> Resources { get; set; } = [];
}
