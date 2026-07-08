namespace IconReplacer.Core;

public sealed record IconCategory(string Name, string FullPath)
{
    public static IconCategory Imported(string libraryRoot)
    {
        return new IconCategory(
            IconLibraryPaths.ImportedFolderName,
            Path.Combine(libraryRoot, IconLibraryPaths.ImportedFolderName));
    }
}

