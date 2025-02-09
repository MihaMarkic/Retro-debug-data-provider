namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record Label(string Name, bool IsMultiOccurrence)
{
    public string FullName => IsMultiOccurrence ? $"!{Name}" : Name;
}

public record Constant(string Name, string Value);
public record EnumValues(ImmutableList<EnumValue> Values);
public record EnumValue(string Name, string? Value = null);