using System.Net.Http.Json;
using System.Text.Json;

// Handles the OpenXBL "Sign in with Xbox" OAuth flow.
//
//   1. Send the user to GetLoginUrl(). They sign in with their Microsoft/Xbox
//      account and are redirected to the app's callback with a ?code=... param.
//   2. ClaimAsync(code) exchanges that code for the user's own personal API key
//      plus their profile (gamertag / XUID).
//
// The app's public key comes from the XBL_APP_KEY env var (the "Public Key" shown
// on the Apps page at https://xbl.io/).
public sealed class OpenXblAuthClient
{
    private readonly HttpClient _http;
    private readonly string _appKey;

    public OpenXblAuthClient(HttpClient http)
    {
        _http = http;
        _appKey = Environment.GetEnvironmentVariable("XBL_APP_KEY")
            ?? throw new InvalidOperationException(
                "Set the XBL_APP_KEY environment variable (your app's public key from https://xbl.io/).");
    }

    /// <summary>The URL to send the user to so they can sign in with their Xbox account.</summary>
    public string GetLoginUrl() => $"https://xbl.io/app/auth/{_appKey}";

    /// <summary>Exchange the code from the OAuth redirect for the user's key + profile.</summary>
    public async Task<ClaimResult> ClaimAsync(string code, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://xbl.io/app/claim")
        {
            Content = JsonContent.Create(new { code, app_key = _appKey })
        };
        req.Headers.Add("Accept", "application/json");

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        var apiKey = FindString(data, "app_key", "apiKey", "api_key", "key");
        if (apiKey is null)
            throw new InvalidOperationException(
                "Claim succeeded but no API key was found in the response. Raw response: " + data);

        return new ClaimResult(
            apiKey,
            FindString(data, "gamertag", "gtg"),
            FindString(data, "xuid", "xid"),
            data);
    }

    // The exact claim-response shape is undocumented and may nest profile fields,
    // so search the whole tree for the first matching property name.
    private static string? FindString(JsonElement el, params string[] names)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String &&
                        names.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        var s = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) return s;
                    }

                    var nested = FindString(prop.Value, names);
                    if (nested is not null) return nested;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                {
                    var nested = FindString(item, names);
                    if (nested is not null) return nested;
                }
                break;
        }

        return null;
    }
}

public sealed record ClaimResult(string ApiKey, string? Gamertag, string? Xuid, JsonElement Raw);
