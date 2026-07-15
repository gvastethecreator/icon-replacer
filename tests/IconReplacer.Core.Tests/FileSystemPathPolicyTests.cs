using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class FileSystemPathPolicyTests
{
    [WindowsOnlyFact]
    public void ValidateExistingLocalDirectoryRejectsLocalLinkToRemoteTarget()
    {
        using var temp = new TempDirectory();
        var link = temp.PathFor("remote-link");

        try
        {
            ReparsePointTestHelper.CreateDirectoryLink(
                DirectoryLinkKind.SymbolicLink,
                link,
                @"\\icon-replacer.invalid\share");

            var result = FileSystemPathPolicy.ValidateExistingLocalDirectory(link);

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.RemotePathUnsupported, result.Error.Code);
            Assert.Contains("remote", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            ReparsePointTestHelper.DeleteDirectoryLink(link);
        }
    }

    [WindowsOnlyFact]
    public void ValidateExistingLocalDirectoryRejectsLinkCycle()
    {
        using var temp = new TempDirectory();
        var first = temp.PathFor("first");
        var second = temp.PathFor("second");

        try
        {
            ReparsePointTestHelper.CreateDirectoryLink(DirectoryLinkKind.SymbolicLink, first, second);
            ReparsePointTestHelper.CreateDirectoryLink(DirectoryLinkKind.SymbolicLink, second, first);

            var result = FileSystemPathPolicy.ValidateExistingLocalDirectory(first);

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.UnsupportedTarget, result.Error.Code);
            Assert.Contains("cycle", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            ReparsePointTestHelper.DeleteDirectoryLink(first);
            ReparsePointTestHelper.DeleteDirectoryLink(second);
        }
    }

    [WindowsOnlyFact]
    public void ValidateExistingLocalDirectoryRejectsExcessiveLinkDepth()
    {
        using var temp = new TempDirectory();
        var links = Enumerable.Range(0, 33)
            .Select(index => temp.PathFor($"link-{index:D2}"))
            .ToArray();
        var target = temp.PathFor("target");
        Directory.CreateDirectory(target);

        try
        {
            for (var index = 0; index < links.Length; index++)
            {
                var next = index == links.Length - 1 ? target : links[index + 1];
                ReparsePointTestHelper.CreateDirectoryLink(
                    DirectoryLinkKind.SymbolicLink,
                    links[index],
                    next);
            }

            var result = FileSystemPathPolicy.ValidateExistingLocalDirectory(links[0]);

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.UnsupportedTarget, result.Error.Code);
            Assert.Contains("too deep", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            foreach (var link in links)
            {
                ReparsePointTestHelper.DeleteDirectoryLink(link);
            }
        }
    }
}
