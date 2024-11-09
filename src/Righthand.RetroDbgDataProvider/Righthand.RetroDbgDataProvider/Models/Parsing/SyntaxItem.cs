namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record SyntaxItem(int Start, int End, TokenType TokenType);
public record FileReferenceSyntaxItem(int Start, int End, string Path): SyntaxItem(Start, End, TokenType.FileReference);