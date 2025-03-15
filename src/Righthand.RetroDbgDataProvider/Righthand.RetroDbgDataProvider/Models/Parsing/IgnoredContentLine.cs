using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represnets a KickAssembler ignored content.
/// </summary>
/// <param name="Items"></param>
public record IgnoredContentLine(ImmutableArray<SingleLineTextRange> Items);