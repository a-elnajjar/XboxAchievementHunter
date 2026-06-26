# 🎮 Xbox Achievement Hunter — MCP Server

A [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server, written in C# / .NET,
that lets an MCP client like **Claude Desktop** look up Xbox players and (as it grows) help hunt
achievements. It pulls data from the [OpenXBL](https://xbl.io/) Xbox Live API.

> **Status:** starting simple — sign in with your Xbox account and look players up. More tools
> (find unearned achievements, easy wins, how-to-earn guidance) are added incrementally.

---

## What it does today

| Tool | Description |
|------|-------------|
| `SignInWithXbox` | Start signing in with your Xbox/Microsoft account → returns a login URL |
| `CompleteSignIn` | Finish sign-in: exchange the redirect `code` for your identity + API key |
| `WhoAmI` | Show who is currently signed in (gamertag + XUID) |
| `GetPlayerProfile` | Look up an Xbox player by gamertag → profile, XUID, and gamerscore |
| `ListPlayedTitles` | List the games a player has played, each with its title ID |
| `GetPlayerPresence` | What a player is currently doing on Xbox (presence) |
| `GetActivityHistory` | Recent Xbox activity for the signed-in account |
| `GetActivityFeed` | Social activity feed for the signed-in account |

## How the project is laid out

```
Claude → GetPlayerProfile (Achievementtools.cs) → SearchGamertagAsync (Openxblclient.cs) → OpenXBL API
```

| File | Role |
|------|------|
| `Program.cs` | Starts the server, sets up stdio transport and dependency injection |
| `Openxblclient.cs` | Talks to the OpenXBL API — one method per endpoint |
| `OpenXblAuthClient.cs` | Runs the "Sign in with Xbox" OAuth flow (login URL + code → API key) |
| `XblAuthSession.cs` | Holds the current session's API key in memory (set by sign-in) |
| `Achievementtools.cs` | The tools Claude can see — each `[McpServerTool]` method is callable |
| `XboxAchievementHunter.csproj` | Project + package references |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (the SDK runs on .NET 8/9/10; tooling prefers 10)
- An **OpenXBL app public key** for "Sign in with Xbox" — see below. (A shared
  **OpenXBL API key** in `XBL_API_KEY` still works as an optional fallback. Free tier:
  150 requests/hour.)

## Sign in with Xbox (OpenXBL OAuth)

Instead of hard-coding one API key, each user can sign in with their own Microsoft/Xbox
account. The server then knows *their* gamertag and XUID, and other tools default to them.

**One-time setup — create an OpenXBL app:**

1. Sign in at https://xbl.io/ and open the **Apps** section.
2. Create an app and copy its **Public Key**.
3. Set the app's redirect/callback URL (the default `https://xbl.io/app/callback` is fine).
4. Provide the public key to the server via the `XBL_APP_KEY` env var (config `env` block).

**Signing in (per session — kept in memory only, no tokens written to disk):**

1. Ask Claude to sign you in. It calls `SignInWithXbox`, which returns a login URL.
2. Open the URL, sign in with your Xbox/Microsoft account, and approve.
3. You're redirected to the callback page — copy the `code` value from that page's URL.
4. Give the code back to Claude; it calls `CompleteSignIn`, which replies
   *"Signed in as &lt;your gamertag&gt; (XUID …)."*
5. `WhoAmI` confirms who's signed in at any time.

## Build

```bash
cd XboxAchievementHunter
dotnet build -c Release
```

You want **"Build succeeded. 0 Error(s)."**

## Run & test standalone (optional)

Test the tools in isolation with the MCP Inspector before wiring it into Claude:

```bash
export XBL_APP_KEY="your-openxbl-app-public-key"
npx @modelcontextprotocol/inspector dotnet run
```

A local web UI opens — call `SignInWithXbox`, follow the URL, sign in, then call
`CompleteSignIn` with the `code` from the redirect. Then try `GetPlayerProfile` with a
gamertag like `Major Nelson`.

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
         "env": { "XBL_APP_KEY": "your-openxbl-app-public-key" }
       }
     }
   }
   ```

   > `XBL_API_KEY` is optional — set it instead of (or alongside) `XBL_APP_KEY` to start
   > with a shared key and skip the sign-in step.

3. Save, then **fully quit Claude (Cmd-Q — not just the window)** and reopen.

4. In a chat, try:

   > Sign me in to Xbox

   Follow the link it gives you, then paste back the `code` from the redirect URL.

> **Windows:** the config path is `%APPDATA%\Claude\claude_desktop_config.json`, and backslashes
> in the DLL path must be escaped (`C:\\Users\\...`).

## Troubleshooting

- **Server doesn't appear / tools missing** → open **Settings → Developer** in Claude; the server's
  status and any error are shown there.
- **Invalid JSON / "non-whitespace after JSON"** → the config has a stray brace or text after the
  final `}`. The file must be one clean JSON object. Validate it at https://jsonlint.com.
- **Build fails** → the server can't start, so the tools vanish. Fix the build first.
- **"Not signed in" / "XBL_APP_KEY not set"** → Claude launches the server itself, so the key must
  be in the config's `env` block, not just your shell. Sign in with `SignInWithXbox`, or set
  `XBL_API_KEY` as a fallback.
- **Path errors** → use an absolute path to the built `.dll`; confirm it exists with `ls`.

## Roadmap

Tools to add next (each verified by rebuilding after adding it):

- `FindUnearned` — locked achievements for a game, rarest-first
- `SuggestEasyWins` — locked achievements most players already have
- `HowToEarn` — factual requirements the assistant turns into step-by-step guidance

## Security

- Keep keys out of source control (use the env block / `.gitignore`).
- Sign-in credentials are held in memory only for the session — nothing is written to disk.
- Treat every tool argument as untrusted input — it comes from an LLM.

## Disclaimer

OpenXBL is an unofficial Xbox Live API, not affiliated with Microsoft. "Xbox" is a trademark of
Microsoft. This project is for personal/educational use.
