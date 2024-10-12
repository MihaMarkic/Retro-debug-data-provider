using System.Collections.Frozen;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

public class KickAssemblerParsedSourceFile : ParsedSourceFile
{
    public KickAssemblerLexer Lexer { get; init; }
    public CommonTokenStream CommonTokenStream { get; init; }
    public KickAssemblerParser Parser { get; init; }
    public bool IsImportOnce { get; }
    public override Lazy<ImmutableArray<TextRange>> IgnoredDefineContent { get; }

    public KickAssemblerParsedSourceFile(
        string fileName,
        FrozenSet<string> referencedFiles,
        FrozenSet<string> inDefines,
        FrozenSet<string> outDefines,
        DateTimeOffset lastModified,
        string? liveContent,
        KickAssemblerLexer lexer,
        CommonTokenStream commonTokenStream,
        KickAssemblerParser parser,
        bool isImportOnce
    ) : base(fileName, referencedFiles, inDefines, outDefines, lastModified, liveContent)
    {
        Lexer = lexer;
        CommonTokenStream = commonTokenStream;
        Parser = parser;
        IsImportOnce = isImportOnce;
        IgnoredDefineContent = new(GetIgnoredDefineContent, LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>
    /// Collects all ignored ranges and merges them if they are continuous.
    /// </summary>
    /// <returns>An array of <see cref="TextRange"/> values.</returns>
    internal ImmutableArray<TextRange> GetIgnoredDefineContent()
    {
        var builder = ImmutableArray.CreateBuilder<TextRange>();
        IToken? startToken = null;
        IToken? previousToken = null;
        foreach (var t in CommonTokenStream.GetTokens()
                     .Where(t => t.Channel == KickAssemblerLexer.IGNORED))
        {
            if (startToken is null || previousToken is null)
            {
                previousToken = startToken = t;
            }
            else
            {
                // in case of continous range, extend current one 
                if (previousToken.StopIndex == t.StartIndex - 1)
                {
                    previousToken = t;
                }
                else
                {
                    builder.Add(new TextRange(
                        new TextCursor(startToken.Line, startToken.Column),
                        new TextCursor(previousToken.Line, previousToken.Column + previousToken.Text.Length)));
                    startToken = previousToken = t;
                }
            }
        }

        if (startToken is not null && previousToken is not null)
        {
            builder.Add(new TextRange(
                new TextCursor(startToken.Line, startToken.Column),
                new TextCursor(previousToken.Line, previousToken.Column + previousToken.Text.Length)));
        }
        return builder.ToImmutable();
    }
}