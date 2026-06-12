# 🎮 Xbox Achievement Hunter — MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server, written in C# / .NET,
that lets an MCP client like **Claude Desktop** look up Xbox players and (as it grows) help hunt
achievements. It pulls data from the [OpenXBL](https://xbl.io/) Xbox Live API.

> **Status:** starting simple — one working tool (`GetPlayerProfile`). More tools
> (find unearned achievements, easy wins, how-to-earn guidance) are added incrementally.

---

## What it does today

| Tool | Description |
|------|-------------|
| `GetPlayerProfile` | Look up an Xbox player by gamertag → profile, XUID, and gamerscore |

## How the project is laid out

The server is two small parts that work as a pair:

```
Claude → GetPlayerProfile (AchievementTools.cs) → SearchGamertagAsync (OpenXblClient.cs) → OpenXBL API
```

| File | Role |
|------|------|
| `Program.cs` | Starts the server, sets up stdio transport and dependency injection |
| `OpenXblClient.cs` | Talks to the OpenXBL API — one method per endpoint |
| `AchievementTools.cs` | The tools Claude can see — each `[McpServerTool]` method is callable |
| `XboxAchievementHunter.csproj` | Project + package references |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (the SDK runs on .NET 8/9/10; tooling prefers 10)
- A free **OpenXBL API key** — sign in at https://xbl.io/ with your Microsoft (Xbox) account
  and copy the key from your profile page. Free tier: 150 requests/hour.

## Build

```bash
cd XboxAchievementHunter
dotnet build -c Release
```

You want **"Build succeeded. 0 Error(s)."**

## Run & test standalone (optional)

Test the tool in isolation with the MCP Inspector before wiring it into Claude:

```bash
export XBL_API_KEY="your-openxbl-key"
npx @modelcontextprotocol/inspector dotnet run
```

A local web UI opens — pick `GetPlayerProfile`, enter a gamertag like `Major Nelson`, and run it.

## Connect to Claude Desktop (macOS)

1. The config file lives at:
   `~/Library/Application Support/Claude/claude_desktop_config.json`
   Open it directly (the `~/Library` folder is hidden in Finder):

   ```bash
   open -e "$HOME/Library/Application Support/Claude/claude_desktop_config.json"
   ```

2. Add your server (replace the path and key). If the file already has other `mcpServers`,
   add this entry inside the existing block rather than creating a second one:

   ```json
   {
     "mcpServers": {
       "xbox-achievements": {
         "command": "dotnet",
         "args": ["/Users/YOU/Documents/GitHub/XboxAchievementHunter/bin/Release/net10.0/XboxAchievementHunter.dll"],
         "env": { "XBL_API_KEY": "your-openxbl-key" }
       }
     }
   }
   ```

3. Save, then **fully quit Claude (Cmd-Q — not just the window)** and reopen.

4. In a chat, try:

   > Look up the Xbox gamertag "Major Nelson"

   A profile coming back means it's wired up correctly.

> **Windows:** the config path is `%APPDATA%\Claude\claude_desktop_config.json`, and backslashes
> in the DLL path must be escaped (`C:\\Users\\...`).

## Troubleshooting

- **Server doesn't appear / tools missing** → open **Settings → Developer** in Claude; the server's
  status and any error are shown there.
- **Invalid JSON / "non-whitespace after JSON"** → the config has a stray brace or text after the
  final `}`. The file must be one clean JSON object. Validate it at https://jsonlint.com.
- **Build fails** → the server can't start, so the tools vanish. Fix the build first.
- **"XBL_API_KEY not set"** → Claude launches the server itself, so the key must be in the config's
  `env` block, not just your shell.
- **Path errors** → use an absolute path to the built `.dll`; confirm it exists with `ls`.

## Roadmap

Tools to add next (each verified by rebuilding after adding it):

- `ListPlayedTitles` — find a game's title ID from the player's library
- `FindUnearned` — locked achievements for a game, rarest-first
- `SuggestEasyWins` — locked achievements most players already have
- `HowToEarn` — factual requirements the assistant turns into step-by-step guidance

## Security

- Keep your `XBL_API_KEY` out of source control (use the env block / `.gitignore`).
- Treat every tool argument as untrusted input — it comes from an LLM.

## Disclaimer

OpenXBL is an unofficial Xbox Live API, not affiliated with Microsoft. "Xbox" is a trademark of
Microsoft. This project is for personal/educational use.# 🎮 Xbox Achievement Hunter — MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server, written in C# / .NET,
that lets an MCP client like **Claude Desktop** look up Xbox players and (as it grows) help hunt
achievements. It pulls data from the [OpenXBL](https://xbl.io/) Xbox Live API.

> **Status:** starting simple — one working tool (`GetPlayerProfile`). More tools
> (find unearned achievements, easy wins, how-to-earn guidance) are added incrementally.

---

## What it does today

| Tool | Description |
|------|-------------|
| `GetPlayerProfile` | Look up an Xbox player by gamertag → profile, XUID, and gamerscore |

## How the project is laid out

The server is two small parts that work as a pair:

```
Claude → GetPlayerProfile (AchievementTools.cs) → SearchGamertagAsync (OpenXblClient.cs) → OpenXBL API
```

| File | Role |
|------|------|
| `Program.cs` | Starts the server, sets up stdio transport and dependency injection |
| `OpenXblClient.cs` | Talks to the OpenXBL API — one method per endpoint |
| `AchievementTools.cs` | The tools Claude can see — each `[McpServerTool]` method is callable |
| `XboxAchievementHunter.csproj` | Project + package references |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (the SDK runs on .NET 8/9/10; tooling prefers 10)
- A free **OpenXBL API key** — sign in at https://xbl.io/ with your Microsoft (Xbox) account
  and copy the key from your profile page. Free tier: 150 requests/hour.

## Build

```bash
cd XboxAchievementHunter
dotnet build -c Release
```

You want **"Build succeeded. 0 Error(s)."**

## Run & test standalone (optional)

Test the tool in isolation with the MCP Inspector before wiring it into Claude:

```bash
export XBL_API_KEY="your-openxbl-key"
npx @modelcontextprotocol/inspector dotnet run
```

A local web UI opens — pick `GetPlayerProfile`, enter a gamertag like `Major Nelson`, and run it.

## Connect to Claude Desktop (macOS)

1. The config file lives at:
   `~/Library/Application Support/Claude/claude_desktop_config.json`
   Open it directly (the `~/Library` folder is hidden in Finder):

   ```bash
   open -e "$HOME/Library/Application Support/Claude/claude_desktop_config.json"
   ```

2. Add your server (replace the path and key). If the file already has other `mcpServers`,
   add this entry inside the existing block rather than creating a second one:

   ```json
   {
     "mcpServers": {
       "xbox-achievements": {
         "command": "dotnet",
         "args": ["/Users/YOU/Documents/GitHub/XboxAchievementHunter/bin/Release/net10.0/XboxAchievementHunter.dll"],
         "env": { "XBL_API_KEY": "your-openxbl-key" }
       }
     }
   }
   ```

3. Save, then **fully quit Claude (Cmd-Q — not just the window)** and reopen.

4. In a chat, try:

   > Look up the Xbox gamertag "Major Nelson"

   A profile coming back means it's wired up correctly.

> **Windows:** the config path is `%APPDATA%\Claude\claude_desktop_config.json`, and backslashes
> in the DLL path must be escaped (`C:\\Users\\...`).

## Troubleshooting

- **Server doesn't appear / tools missing** → open **Settings → Developer** in Claude; the server's
  status and any error are shown there.
- **Invalid JSON / "non-whitespace after JSON"** → the config has a stray brace or text after the
  final `}`. The file must be one clean JSON object. Validate it at https://jsonlint.com.
- **Build fails** → the server can't start, so the tools vanish. Fix the build first.
- **"XBL_API_KEY not set"** → Claude launches the server itself, so the key must be in the config's
  `env` block, not just your shell.
- **Path errors** → use an absolute path to the built `.dll`; confirm it exists with `ls`.

## Roadmap

Tools to add next (each verified by rebuilding after adding it):

- `ListPlayedTitles` — find a game's title ID from the player's library
- `FindUnearned` — locked achievements for a game, rarest-first
- `SuggestEasyWins` — locked achievements most players already have
- `HowToEarn` — factual requirements the assistant turns into step-by-step guidance

## Security

- Keep your `XBL_API_KEY` out of source control (use the env block / `.gitignore`).
- Treat every tool argument as untrusted input — it comes from an LLM.

## Disclaimer

OpenXBL is an unofficial Xbox Live API, not affiliated with Microsoft. "Xbox" is a trademark of
Microsoft. This project is for personal/educational use.