using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconImportResult(
    IconLibraryEntry ImportedIcon,
    IconLibraryStatus LibraryStatus);
