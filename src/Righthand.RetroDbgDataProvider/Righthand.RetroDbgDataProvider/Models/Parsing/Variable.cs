namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record Variable(string Name, VariableType VariableType, RangeInFile? Range);

public enum VariableType
{
    Global,
    For,
}