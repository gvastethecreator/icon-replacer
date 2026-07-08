using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconPickerRequestServiceTests
{
    [Fact]
    public void CreateRequestBuildsSingleIconPickerForSupportedFolder()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Projects");
        Directory.CreateDirectory(folder);

        var request = new IconPickerRequestService().CreateRequest(folder, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanOpenPicker);
        Assert.Equal(ErrorCode.None, request.Value.Error.Code);
        Assert.Equal(ShellSelectionStatus.Supported, request.Value.Selection.Status);
        Assert.Equal(TargetKind.Folder, request.Value.Selection.Target!.Kind);
        Assert.Equal(paths.LibraryRoot, request.Value.InitialDirectory);
        Assert.Equal("Icon files (*.ico)", request.Value.FileTypeLabel);
        Assert.Contains(".ico", request.Value.FileExtensions);
        Assert.False(request.Value.AllowMultiple);
        Assert.True(Directory.Exists(paths.LibraryRoot));
        Assert.True(Directory.Exists(paths.ImportedRoot));
    }

    [Fact]
    public void CreateRequestRejectsUnsupportedTargetWithoutPreparingPickerFolders()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var file = temp.PathFor("notes.txt");
        File.WriteAllText(file, "not a shortcut");

        var request = new IconPickerRequestService().CreateRequest(file, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.False(request.Value.CanOpenPicker);
        Assert.Equal(ShellSelectionStatus.UnsupportedTarget, request.Value.Selection.Status);
        Assert.Equal(ErrorCode.UnsupportedTarget, request.Value.Error.Code);
        Assert.False(Directory.Exists(paths.LibraryRoot));
        Assert.False(Directory.Exists(paths.ImportedRoot));
    }

    [Fact]
    public void CreateRequestRejectsMultipleSelection()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new IconPickerRequestService().CreateRequest(
            [
                new ShellSelectionItem(temp.PathFor("one"), IsDirectory: true),
                new ShellSelectionItem(temp.PathFor("two.lnk"), IsDirectory: false)
            ],
            paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.False(request.Value.CanOpenPicker);
        Assert.Equal(ShellSelectionStatus.MultipleSelectionUnsupported, request.Value.Selection.Status);
        Assert.Equal(ErrorCode.UnsupportedTarget, request.Value.Error.Code);
        Assert.Equal("2 selected items", request.Value.RequestedTargetPath);
    }
}
