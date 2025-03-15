namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents a KickAssembler file reference syntax.
/// </summary>
/// <param name="Start"></param>
/// <param name="End"></param>
/// <param name="ReferencedFile"></param>
public record FileReferenceSyntaxItem(int Start, int End, ReferencedFileInfo ReferencedFile)
    : SyntaxItem(Start, End, TokenType.FileReference);