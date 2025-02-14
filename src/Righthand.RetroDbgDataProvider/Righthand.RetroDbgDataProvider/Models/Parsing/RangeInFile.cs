namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record RangeInFile(Position? Start, Position? End);
public record Position(int Line, int Column);