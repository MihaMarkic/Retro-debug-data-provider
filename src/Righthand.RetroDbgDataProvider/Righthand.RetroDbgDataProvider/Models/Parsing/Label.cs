namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record Label(string Name, bool IsMultiOccurrence)
{
    public string FullName => IsMultiOccurrence ? $"!{Name}" : Name;
}