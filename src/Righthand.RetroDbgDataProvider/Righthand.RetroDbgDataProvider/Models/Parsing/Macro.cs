namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record Macro(string Name, bool IsScopeEsc, ImmutableList<string> Arguments);