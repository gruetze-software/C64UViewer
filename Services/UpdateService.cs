using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace C64UViewer.Services;

internal static class UpdateService
{
    private const string VersionUrl = "https://api.github.com/repos/gruetze-software/C64UViewer/releases/latest";

    public static async Task<(bool Available, string Url, string Tag)> CheckForUpdatesAsync(
        Version currentVersion, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("C64UViewer-Updater");

        try
        {
            var release = await client.GetFromJsonAsync<GitHubRelease>(VersionUrl, cancellationToken);
            if (release?.TagName is string tag
                && Version.TryParse(tag.Trim().TrimStart('v', 'V'), out var latest)
                && Normalize(latest) > Normalize(currentVersion)
                && Uri.TryCreate(release.HtmlUrl, UriKind.Absolute, out var url)
                && url.Scheme == Uri.UriSchemeHttps && url.Host == "github.com")
            {
                return (true, url.AbsoluteUri, tag);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            if (!cancellationToken.IsCancellationRequested)
                Trace.WriteLine($"Update check failed: {ex.Message}");
        }

        return (false, string.Empty, string.Empty);
    }

    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("html_url")] string? HtmlUrl);
}
