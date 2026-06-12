using System.Net.Http.Json;
using System.Text.Json;

// The part that talks to the OpenXBL API. One method per endpoint we use.
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

    /// <summary>Resolve a gamertag to a full profile (incl. XUID + gamerscore).</summary>
    public async Task<JsonElement> SearchGamertagAsync(string gamertag, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync($"search/{Uri.EscapeDataString(gamertag)}", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

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
    public async Task<JsonElement> GetPlayerTitlesAsync(string xuid, CancellationToken ct = default)
    {
        // OpenXBL's dedicated "games this player owns" endpoint.
        using var resp = await _http.GetAsync($"player/titleHistory/{xuid}", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }
}