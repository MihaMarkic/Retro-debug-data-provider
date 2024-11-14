namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record FileReferenceSyntaxItem(int Start, int End, ReferencedFileInfo ReferencedFile)
    : SyntaxItem(Start, End, TokenType.FileReference);