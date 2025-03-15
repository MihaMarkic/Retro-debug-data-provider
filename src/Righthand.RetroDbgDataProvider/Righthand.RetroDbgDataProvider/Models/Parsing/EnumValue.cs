namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents a KickAssembler enum value.
/// </summary>
/// <param name="Name"></param>
/// <param name="Value"></param>
public record EnumValue(string Name, string? Value = null);