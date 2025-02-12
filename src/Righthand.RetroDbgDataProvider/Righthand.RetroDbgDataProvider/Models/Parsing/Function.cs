namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record Function(string Name, bool IsScopeEsc, ImmutableList<string> Arguments);