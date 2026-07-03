namespace ComputeSyncOrder;

public sealed record ResourceIdentity(string Kind, string Name, int SyncWave)
{
    public override string ToString() => $"{Kind}/{Name}";
}
