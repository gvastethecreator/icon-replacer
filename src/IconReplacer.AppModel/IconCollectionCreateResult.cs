namespace IconReplacer.AppModel;

public sealed record IconCollectionCreateResult(
    IconCollectionSummary Collection,
    bool Created,
    IconLibraryStatus LibraryStatus);
