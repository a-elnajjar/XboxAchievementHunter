using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

// The tools Claude can see. Each [McpServerTool] method becomes a callable tool.
// PlayerContext is injected so tools can default to the remembered player.
[McpServerToolType]
public static class AchievementTools
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    [McpServerTool]
    [Description("Start signing in with your Xbox/Microsoft account. Returns a URL to open in a " +
                 "browser. After signing in you'll be redirected to a page whose URL contains a " +
                 "'code' value — copy it and call CompleteSignIn with it.")]
    public static string SignInWithXbox(OpenXblAuthClient auth)
        => $"Open this URL in your browser and sign in with your Xbox/Microsoft account:\n\n" +
           $"{auth.GetLoginUrl()}\n\n" +
           "After approving, you'll be redirected to a page (default https://xbl.io/app/callback). " +
           "Copy the 'code' value from that page's URL and call CompleteSignIn with it.";

    [McpServerTool]
    [Description("Finish signing in: exchange the 'code' from the sign-in redirect for your Xbox " +
                 "identity. After this, other tools default to you (no gamertag needed).")]
    public static async Task<string> CompleteSignIn(
        OpenXblAuthClient auth,
        XblAuthSession session,
        PlayerContext player,
        OpenXblClient client,
        [Description("The 'code' value from the sign-in redirect URL.")] string code)
    {
        var result = await auth.ClaimAsync(code);
        session.ApiKey = result.ApiKey;

        player.Gamertag = result.Gamertag ?? player.Gamertag;
        player.Xuid = result.Xuid ?? player.Xuid;

        // If the claim response didn't include identity, backfill it now that we
        // have a working key (e.g. resolve a known gamertag to its XUID).
        if (player.Xuid is null && player.Gamertag is not null)
            player.Xuid = await client.GetXuidAsync(player.Gamertag);

        var who = player.Gamertag is not null
            ? $"Signed in as {player.Gamertag}" + (player.Xuid is not null ? $" (XUID {player.Xuid})." : ".")
            : "Signed in. (Couldn't read your gamertag from the response — other tools still work.)";
        return who;
    }

    [McpServerTool]
    [Description("Show who is currently signed in (gamertag and XUID), or that no one is signed in.")]
    public static string WhoAmI(XblAuthSession session, PlayerContext player)
    {
        if (!session.IsSignedIn)
            return "Not signed in. Call SignInWithXbox to begin.";
        return player.Gamertag is not null
            ? $"Signed in as {player.Gamertag}" + (player.Xuid is not null ? $" (XUID {player.Xuid})." : ".")
            : "Signed in (gamertag unknown).";
    }

    [McpServerTool]
    [Description("Look up an Xbox player by gamertag and return their profile, XUID, and gamerscore. " +
                 "Remembers the player so later tools don't need the XUID again.")]
    public static async Task<string> GetPlayerProfile(
        OpenXblClient client,
        PlayerContext player,
        [Description("The player's Xbox gamertag, e.g. 'Major Nelson'")] string gamertag)
    {
        var data = await client.SearchGamertagAsync(gamertag);

        // Cache the first match's XUID for the rest of the session.
        if (data.TryGetProperty("content", out var content) &&
            content.TryGetProperty("people", out var people) &&
            people.ValueKind == JsonValueKind.Array &&
            people.GetArrayLength() > 0 &&
            people[0].TryGetProperty("xuid", out var x))
        {
            player.Xuid = x.GetString();
            player.Gamertag = gamertag;
        }

        return JsonSerializer.Serialize(data, Pretty);
    }

    [McpServerTool]
    [Description("List the games a player has played, each with its title ID and name. Use this to " +
                 "find the titleId other tools need. If no gamertag/xuid is given, uses the remembered player.")]
    public static async Task<string> ListPlayedTitles(
        OpenXblClient client,
        PlayerContext player,
        [Description("Optional gamertag. Leave empty to use the remembered/default player.")] string? gamertag = null,
        [Description("Optional XUID. Leave empty to use the remembered/default player.")] string? xuid = null)
    {
        var resolved = await ResolveXuidAsync(client, player, xuid, gamertag);
        if (resolved is null)
            return "No player set. Call GetPlayerProfile first, or set XBL_DEFAULT_GAMERTAG in the config.";

        var data = await client.GetPlayerTitlesAsync(resolved);
        if (!data.TryGetProperty("titles", out var titles))
            return "No titles array in the response. Check the XUID.";

        var games = titles.EnumerateArray()
            .Select(t => new
            {
                titleId = GetString(t, "titleId"),
                name = GetString(t, "name")
            })
            .Where(g => g.titleId.Length > 0)
            .OrderBy(g => g.name)
            .ToList();

        return games.Count == 0
            ? "No played titles found for this player."
            : JsonSerializer.Serialize(games, Pretty);
    }
    [McpServerTool]
[Description("Get recent Xbox activity history for the account tied to the API key.")]
public static async Task<string> GetActivityHistory(OpenXblClient client)
{
    var data = await client.GetActivityHistoryAsync();
    return JsonSerializer.Serialize(data, Pretty);
}

[McpServerTool]
[Description("Get the Xbox social activity feed for the account tied to the API key.")]
public static async Task<string> GetActivityFeed(OpenXblClient client)
{
    var data = await client.GetActivityFeedAsync();
    return JsonSerializer.Serialize(data, Pretty);
}

[McpServerTool]
[Description("Get what a player is currently doing on Xbox (presence). Uses remembered player or an optional gamertag/XUID.")]
public static async Task<string> GetPlayerPresence(
    OpenXblClient client,
    PlayerContext player,
    [Description("Optional gamertag. Leave empty to use the remembered/default player.")] string? gamertag = null,
    [Description("Optional XUID. Leave empty to use the remembered/default player.")] string? xuid = null)
{
    var resolved = await ResolveXuidAsync(client, player, xuid, gamertag);
    if (resolved is null)
        return "No player set. Call GetPlayerProfile first, or set XBL_DEFAULT_GAMERTAG in the config.";

    var data = await client.GetPresenceAsync(resolved);
    return JsonSerializer.Serialize(data, Pretty);
}

    // ---- helpers ----

    // Resolution order: explicit xuid -> explicit gamertag -> remembered xuid -> default gamertag.
    private static async Task<string?> ResolveXuidAsync(
        OpenXblClient client, PlayerContext player, string? xuid, string? gamertag)
    {
        if (!string.IsNullOrWhiteSpace(xuid)) return xuid;

        if (!string.IsNullOrWhiteSpace(gamertag))
        {
            var resolved = await client.GetXuidAsync(gamertag);
            if (resolved is not null) { player.Xuid = resolved; player.Gamertag = gamertag; }
            return resolved;
        }

        if (!string.IsNullOrWhiteSpace(player.Xuid)) return player.Xuid;

        if (!string.IsNullOrWhiteSpace(player.Gamertag))
        {
            var resolved = await client.GetXuidAsync(player.Gamertag);
            if (resolved is not null) player.Xuid = resolved;
            return resolved;
        }

        return null;
    }

    private static string GetString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
}