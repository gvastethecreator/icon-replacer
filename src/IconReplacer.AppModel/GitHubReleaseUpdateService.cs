using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace IconReplacer.AppModel;

public enum AppUpdateStatus
{
    UpdateAvailable,
    UpToDate,
    NoPublishedRelease,
    Unavailable
}

public sealed record AppUpdateSnapshot(
    AppUpdateStatus Status,
    Version CurrentVersion,
    Version? LatestVersion,
    string? LatestTag,
    Uri ProjectUri,
    Uri ReleasesUri,
    Uri? LatestReleaseUri,
    string Message);

public sealed class GitHubReleaseUpdateService
{
    public static readonly Uri ProjectUri = new("https://github.com/gvastethecreator/icon-replacer");
    public static readonly Uri ReleasesUri = new("https://github.com/gvastethecreator/icon-replacer/releases");

    private static readonly Uri LatestReleaseApiUri = new(
        "https://api.github.com/repos/gvastethecreator/icon-replacer/releases/latest");
    private static readonly HttpClient SharedClient = new();
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    private readonly HttpClient _client;

    public GitHubReleaseUpdateService(HttpClient? client = null)
    {
        _client = client ?? SharedClient;
    }

    public async Task<AppUpdateSnapshot> CheckAsync(
        Version currentVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);

        if (DistributionChannelPolicy.UpdatesManagedByStore)
        {
            return CreateSnapshot(
                AppUpdateStatus.UpToDate,
                currentVersion,
                latestVersion: NormalizeVersion(currentVersion),
                message: "Updates are managed by Microsoft Store for this installation.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd($"Icon-Replacer/{FormatVersion(currentVersion)}");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        try
        {
            using var response = await _client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return CreateSnapshot(
                    AppUpdateStatus.NoPublishedRelease,
                    currentVersion,
                    message: "No published releases are available yet.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return CreateSnapshot(
                    AppUpdateStatus.Unavailable,
                    currentVersion,
                    message: $"GitHub could not complete the update check ({(int)response.StatusCode}).");
            }

            await using var content = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: timeout.Token);
            var root = document.RootElement;
            if (!root.TryGetProperty("tag_name", out var tagElement) ||
                tagElement.ValueKind != JsonValueKind.String ||
                !TryParseReleaseVersion(tagElement.GetString(), out var latestVersion))
            {
                return CreateSnapshot(
                    AppUpdateStatus.Unavailable,
                    currentVersion,
                    message: "The latest release has an unsupported version tag.");
            }

            var latestTag = tagElement.GetString();
            var releaseUri = TryReadTrustedReleaseUri(root) ?? ReleasesUri;
            if (latestVersion > NormalizeVersion(currentVersion))
            {
                return CreateSnapshot(
                    AppUpdateStatus.UpdateAvailable,
                    currentVersion,
                    latestVersion,
                    latestTag,
                    releaseUri,
                    $"Version {FormatVersion(latestVersion)} is available.");
            }

            return CreateSnapshot(
                AppUpdateStatus.UpToDate,
                currentVersion,
                latestVersion,
                latestTag,
                releaseUri,
                $"You are up to date with version {FormatVersion(currentVersion)}.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateSnapshot(
                AppUpdateStatus.Unavailable,
                currentVersion,
                message: "The update check timed out. Try again when GitHub is reachable.");
        }
        catch (HttpRequestException)
        {
            return CreateSnapshot(
                AppUpdateStatus.Unavailable,
                currentVersion,
                message: "GitHub is unavailable. Check your connection and try again.");
        }
        catch (JsonException)
        {
            return CreateSnapshot(
                AppUpdateStatus.Unavailable,
                currentVersion,
                message: "GitHub returned an update response that could not be read.");
        }
        catch (IOException)
        {
            return CreateSnapshot(
                AppUpdateStatus.Unavailable,
                currentVersion,
                message: "The update response could not be downloaded completely.");
        }
    }

    public static bool TryParseReleaseVersion(string? tag, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var value = tag.Trim().TrimStart('v', 'V');
        var suffix = value.IndexOfAny(['-', '+']);
        if (suffix >= 0)
        {
            value = value[..suffix];
        }

        var segments = value.Split('.');
        if (segments.Length is < 1 or > 4)
        {
            return false;
        }

        var parts = new int[4];
        for (var index = 0; index < segments.Length; index++)
        {
            if (!int.TryParse(segments[index], out parts[index]) || parts[index] < 0)
            {
                return false;
            }
        }

        version = new Version(parts[0], parts[1], parts[2], parts[3]);
        return true;
    }

    public static string FormatVersion(Version version)
    {
        var normalized = NormalizeVersion(version);
        return normalized.Revision == 0
            ? $"{normalized.Major}.{normalized.Minor}.{normalized.Build}"
            : normalized.ToString(4);
    }

    private static AppUpdateSnapshot CreateSnapshot(
        AppUpdateStatus status,
        Version currentVersion,
        Version? latestVersion = null,
        string? latestTag = null,
        Uri? latestReleaseUri = null,
        string message = "") =>
        new(
            status,
            NormalizeVersion(currentVersion),
            latestVersion,
            latestTag,
            ProjectUri,
            ReleasesUri,
            latestReleaseUri,
            message);

    private static Version NormalizeVersion(Version version) =>
        new(
            Math.Max(0, version.Major),
            Math.Max(0, version.Minor),
            Math.Max(0, version.Build),
            Math.Max(0, version.Revision));

    private static Uri? TryReadTrustedReleaseUri(JsonElement root)
    {
        if (!root.TryGetProperty("html_url", out var urlElement) ||
            urlElement.ValueKind != JsonValueKind.String ||
            !Uri.TryCreate(urlElement.GetString(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri;
    }
}
