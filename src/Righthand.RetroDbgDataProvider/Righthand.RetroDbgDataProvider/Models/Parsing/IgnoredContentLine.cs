using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record IgnoredContentLine(ImmutableArray<SingleLineTextRange> Items);