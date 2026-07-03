using BumpImageTag;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: BumpImageTag <kustomization-path> <image-name> <new-tag>");
    return 2;
}

ImageTagBumper.BumpTagInFile(args[0], args[1], args[2]);
Console.WriteLine($"Bumped '{args[1]}' to tag '{args[2]}' in {args[0]}");
return 0;
