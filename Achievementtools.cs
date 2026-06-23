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