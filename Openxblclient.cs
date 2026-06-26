using System.Net.Http.Json;
using System.Text.Json;

// The part that talks to the OpenXBL API. One method per endpoint we use.
// The API key is read per-request from XblAuthSession so it can change when the
// user signs in mid-session (rather than being baked in at construction time).
public sealed class OpenXblClient
{
    private readonly HttpClient _http;
    private readonly XblAuthSession _session;

    public OpenXblClient(HttpClient http, XblAuthSession session)
    {
        _http = http;
        _session = session;
        _http.BaseAddress = new Uri("https://xbl.io/api/v2/");
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>Resolve a gamertag to a full profile (incl. XUID + gamerscore).</summary>
    public Task<JsonElement> SearchGamertagAsync(string gamertag, CancellationToken ct = default)
        => GetJsonAsync($"search/{Uri.EscapeDataString(gamertag)}", ct);

    /// <summary>Resolve a gamertag straight to its XUID (first match), or null if not found.</summary>
    public async Task<string?> GetXuidAsync(string gamertag, CancellationToken ct = default)
    {
        var data = await SearchGamertagAsync(gamertag, ct);
        if (data.TryGetProperty("content", out var content) &&
            content.TryGetProperty("people", out var people) &&
            people.ValueKind == JsonValueKind.Array &&
            people.GetArrayLength() > 0 &&
            people[0].TryGetProperty("xuid", out var x))
            return x.GetString();
        return null;
    }

    /// <summary>The games a player has played (each with titleId + name).</summary>
    public Task<JsonElement> GetPlayerTitlesAsync(string xuid, CancellationToken ct = default)
        // OpenXBL's dedicated "games this player owns" endpoint.
        => GetJsonAsync($"player/titleHistory/{xuid}", ct);

    /// <summary>Recent activity history for the signed-in account.</summary>
    public Task<JsonElement> GetActivityHistoryAsync(CancellationToken ct = default)
        => GetJsonAsync("activity/history", ct);

    /// <summary>Social activity feed for the signed-in account.</summary>
    public Task<JsonElement> GetActivityFeedAsync(CancellationToken ct = default)
        => GetJsonAsync("activity/feed", ct);

    /// <summary>Current presence for one or more players (comma-separated XUIDs).</summary>
    public Task<JsonElement> GetPresenceAsync(string xuid, CancellationToken ct = default)
        => GetJsonAsync($"{xuid}/presence", ct);

    // ---- internals ----

    // Issues a GET with the current session's API key as X-Authorization.
    // Each request builds its own message so the key is always read fresh.
    private async Task<JsonElement> GetJsonAsync(string path, CancellationToken ct)
    {
        if (!_session.IsSignedIn)
            throw new InvalidOperationException(
                "Not signed in — run SignInWithXbox first, or set XBL_API_KEY.");

        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Add("X-Authorization", _session.ApiKey!);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }
}
