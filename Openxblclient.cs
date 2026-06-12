using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Thin wrapper around the OpenXBL (xbl.io) REST gateway.
///
/// Auth: every request needs the header  X-Authorization: {your api key}
/// Get a key + see exact endpoint paths at https://xbl.io/  (free tier: 150 req/hour).
///
/// NOTE: the endpoint paths below match OpenXBL's documented shape, but verify them
/// against the live docs — unofficial APIs move things around occasionally.
/// </summary>
public sealed class OpenXblClient
{
    private readonly HttpClient _http;

    public OpenXblClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://xbl.io/api/v2/");

        var apiKey = Environment.GetEnvironmentVariable("XBL_API_KEY")
            ?? throw new InvalidOperationException(
                "Set the XBL_API_KEY environment variable (get one at https://xbl.io/).");

        _http.DefaultRequestHeaders.Add("X-Authorization", apiKey);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>Resolve a gamertag to a profile (incl. XUID + gamerscore).</summary>
    public async Task<JsonElement> SearchGamertagAsync(string gamertag, CancellationToken ct = default)
        => await GetJsonAsync($"search/{Uri.EscapeDataString(gamertag)}", ct);

    /// <summary>All achievements for one title for the given player (by XUID).</summary>
    public async Task<JsonElement> GetTitleAchievementsAsync(string xuid, string titleId, CancellationToken ct = default)
        => await GetJsonAsync($"achievements/player/{xuid}/{titleId}", ct);

    /// <summary>Recently played titles for a player — useful for "what should I hunt next".</summary>
    public async Task<JsonElement> GetPlayerTitlesAsync(string xuid, CancellationToken ct = default)
        => await GetJsonAsync($"achievements/player/{xuid}", ct);

    private async Task<JsonElement> GetJsonAsync(string path, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(path, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }
}