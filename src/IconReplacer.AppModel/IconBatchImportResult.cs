namespace IconReplacer.AppModel;

public sealed record IconBatchImportResult(
    IReadOnlyList<IconBatchImportItem> Items,
    IconLibraryStatus LibraryStatus)
{
    public int RequestedCount => Items.Count;

    public int ImportedCount => Items.Count(item => item.Status == IconBatchImportItemStatus.Imported);

    public int ReusedExistingCount => Items.Count(item => item.Status == IconBatchImportItemStatus.ReusedExisting);

    public int FailedCount => Items.Count(item => item.Status == IconBatchImportItemStatus.Failed);
}
