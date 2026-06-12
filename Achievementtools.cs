using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

// The tools Claude can see. Each [McpServerTool] method becomes a callable tool.
// Starting simple: just the gamertag lookup. We'll add more once this works.
[McpServerToolType]
public static class AchievementTools
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    [McpServerTool]
    [Description("Look up an Xbox player by gamertag and return their profile, XUID, and gamerscore.")]
    public static async Task<string> GetPlayerProfile(
        OpenXblClient client,
        [Description("The player's Xbox gamertag, e.g. 'Major Nelson'")] string gamertag)
    {
        var data = await client.SearchGamertagAsync(gamertag);
        return JsonSerializer.Serialize(data, Pretty);
    }
}