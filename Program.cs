using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// stdio uses stdout for the protocol — keep all logging on stderr.
builder.Logging.AddConsole(o =>
{
    o.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddHttpClient<OpenXblClient>();
builder.Services.AddHttpClient<OpenXblAuthClient>();

// Holds the OpenXBL API key for this session (in memory only). Seeded from
// XBL_API_KEY if set, then overwritten when the user signs in with their own account.
builder.Services.AddSingleton(_ => new XblAuthSession
{
    ApiKey = Environment.GetEnvironmentVariable("XBL_API_KEY")
});

// Remembers the current player across tool calls so you don't pass the XUID every time.
// Set XBL_DEFAULT_GAMERTAG in the config env block to default to your own tag.
builder.Services.AddSingleton(_ => new PlayerContext
{
    Gamertag = Environment.GetEnvironmentVariable("XBL_DEFAULT_GAMERTAG")
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();