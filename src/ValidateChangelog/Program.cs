using ValidateChangelog;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ValidateChangelog <path-to-CHANGELOG.md> <version>");
    return 2;
}

var result = ChangelogValidator.ValidateFile(args[0], args[1]);
Console.WriteLine(result.Message);
return result.IsValid ? 0 : 1;
