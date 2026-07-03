using ComputeSyncOrder;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: ComputeSyncOrder <kustomization-directory>");
    return 2;
}

var order = SyncOrderCalculator.ComputeOrder(args[0]);

Console.WriteLine("Deployment order (by sync-wave):");
foreach (var resource in order)
{
    Console.WriteLine($"  wave {resource.SyncWave}: {resource}");
}

return 0;
