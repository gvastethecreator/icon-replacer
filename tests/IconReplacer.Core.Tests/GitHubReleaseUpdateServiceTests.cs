using System.Net;
using System.Text;
using IconReplacer.AppModel;

namespace IconReplacer.Core.Tests;

public sealed class GitHubReleaseUpdateServiceTests
{
    [Fact]
    public async Task CheckReportsNewerPublishedRelease()
    {
        using var client = CreateClient(HttpStatusCode.OK, """
            {
              "tag_name": "v1.2.0",
              "html_url": "https://github.com/gvastethecreator/icon-replacer/releases/tag/v1.2.0"
            }
            """);

        var result = await new GitHubReleaseUpdateService(client).CheckAsync(new Version(1, 0, 0, 0));

        Assert.Equal(AppUpdateStatus.UpdateAvailable, result.Status);
        Assert.Equal(new Version(1, 2, 0, 0), result.LatestVersion);
        Assert.Equal("v1.2.0", result.LatestTag);
        Assert.Equal(
            "https://github.com/gvastethecreator/icon-replacer/releases/tag/v1.2.0",
            result.LatestReleaseUri!.AbsoluteUri.TrimEnd('/'));
    }

    [Fact]
    public async Task CheckReportsCurrentReleaseWithoutFalseUpdate()
    {
        using var client = CreateClient(HttpStatusCode.OK, """
            {
              "tag_name": "1.0.0",
              "html_url": "https://example.com/untrusted-release"
            }
            """);

        var result = await new GitHubReleaseUpdateService(client).CheckAsync(new Version(1, 0, 0, 0));

        Assert.Equal(AppUpdateStatus.UpToDate, result.Status);
        Assert.Equal(GitHubReleaseUpdateService.ReleasesUri, result.LatestReleaseUri);
    }

    [Fact]
    public async Task CheckTreatsMissingPublishedReleaseAsValidState()
    {
        using var client = CreateClient(HttpStatusCode.NotFound, "{}");

        var result = await new GitHubReleaseUpdateService(client).CheckAsync(new Version(1, 0, 0, 0));

        Assert.Equal(AppUpdateStatus.NoPublishedRelease, result.Status);
        Assert.Null(result.LatestVersion);
        Assert.Contains("No published releases", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckRejectsMalformedReleasePayloadWithoutThrowing()
    {
        using var client = CreateClient(HttpStatusCode.OK, """{"tag_name": 100}""");

        var result = await new GitHubReleaseUpdateService(client).CheckAsync(new Version(1, 0, 0, 0));

        Assert.Equal(AppUpdateStatus.Unavailable, result.Status);
        Assert.Contains("unsupported version tag", result.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("v2.3.4", 2, 3, 4, 0)]
    [InlineData("1.2.3.4", 1, 2, 3, 4)]
    [InlineData("v5.0.0+build.7", 5, 0, 0, 0)]
    public void ReleaseTagsAreNormalized(
        string tag,
        int major,
        int minor,
        int build,
        int revision)
    {
        Assert.True(GitHubReleaseUpdateService.TryParseReleaseVersion(tag, out var version));
        Assert.Equal(new Version(major, minor, build, revision), version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("v1.2.3.4.5")]
    public void UnsupportedReleaseTagsAreRejected(string tag)
    {
        Assert.False(GitHubReleaseUpdateService.TryParseReleaseVersion(tag, out _));
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode, string body)
    {
        return new HttpClient(new StubHandler(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            }));
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains(request.Headers.UserAgent, item => item.Product?.Name == "Icon-Replacer");
            return Task.FromResult(response);
        }
    }
}
