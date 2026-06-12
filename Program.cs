using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// CRITICAL for stdio transport: stdout is the MCP protocol channel.
// All logging MUST go to stderr or you'll corrupt the JSON-RPC stream.
builder.Logging.AddConsole(o =>
{
    o.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Typed HttpClient for the OpenXBL gateway.
builder.Services.AddHttpClient<OpenXblClient>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()       // local. swap for ASP.NET Core + Streamable HTTP to go remote.
    .WithToolsFromAssembly();         // discovers every [McpServerTool] in this assembly.

await builder.Build().RunAsync();