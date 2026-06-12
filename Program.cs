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