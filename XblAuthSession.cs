// Holds the OpenXBL API key for the current MCP session, in memory only.
//
// Two ways it gets populated:
//   1. Seeded from the XBL_API_KEY env var at startup (shared-key fallback).
//   2. Overwritten when the user signs in via SignInWithXbox / CompleteSignIn,
//      which swaps in that user's own personal key for the rest of the session.
//
// Nothing here is persisted to disk — restarting the server clears it.
public sealed class XblAuthSession
{
    public string? ApiKey { get; set; }

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(ApiKey);
}
