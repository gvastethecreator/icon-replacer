using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconBatchImportItem(
    string SourcePath,
    IconBatchImportItemStatus Status,
    IconLibraryEntry? ImportedIcon,
    IconReplacerError Error)
{
    public bool Succeeded => Status is
        IconBatchImportItemStatus.Imported or
        IconBatchImportItemStatus.ReusedExisting;
}
