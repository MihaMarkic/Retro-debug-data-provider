using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class FileReferenceCompletionOptions
{
    /// <summary>
    /// Returns possible completion for file references.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="line"></param>
    /// <param name="trigger"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> tokens,
        ReadOnlySpan<char> line, TextChangeTrigger trigger, int column)
    {
        var leftLinePart = line[..(column+1)];
        var (isMatch, doubleQuoteColumn) = GetFileReferenceSuggestion(tokens, leftLinePart, trigger);
        if (isMatch)
        {
            var suggestionLine = line[(doubleQuoteColumn+1)..];
            var (rootText, length, endsWithDoubleQuote) =
                CompletionOptionCollectorsCommon.GetSuggestionTextInDoubleQuotes(suggestionLine, column - doubleQuoteColumn);
            return new CompletionOption(CompletionOptionType.FileReference, rootText, endsWithDoubleQuote, length, []);
        }

        return null;
    }
    /// <summary>
    /// Returns status whether line is valid for file reference suggestions or not.
    /// Also includes index of first double quotes to the left of the column.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="line"></param>
    /// <param name="trigger"></param>
    /// <returns></returns>
    internal static (bool IsMatch, int DoubleQuoteColumn) GetFileReferenceSuggestion(
        ReadOnlySpan<IToken> tokens, ReadOnlySpan<char> line, TextChangeTrigger trigger)
    {
        // check obvious conditions
        if (line.Length == 0 || trigger == TextChangeTrigger.CharacterTyped && line[^1] != '\"')
        {
            return (false, -1);
        }

        int doubleQuoteIndex = line.Length - 1;
        // finds first double quote on the left
        while (doubleQuoteIndex >= 0 && line[doubleQuoteIndex] != '\"')
        {
            doubleQuoteIndex--;
        }

        if (doubleQuoteIndex < 0)
        {
            return (false, -1);
        }
        // there should be only one double quote on the left
        if (line[..doubleQuoteIndex].Contains('\"'))
        {
            return (false, -1);
        }
        // check first token now
        var trimmedLine = CompletionOptionCollectorsCommon.TrimWhitespaces(tokens);
        if (trimmedLine.Length == 0)
        {
            return (false, -1);
        }

        var firstToken = trimmedLine[0]; 
        if (firstToken.Type is not (KickAssemblerLexer.HASHIMPORT or KickAssemblerLexer.HASHIMPORTIF))
        {
            return (false, -1);
        }
        var secondToken = trimmedLine[1];
        
        switch (firstToken.Type)
        {
            // when #import tolerate only WS tokens between keyword and double quotes
            case KickAssemblerLexer.HASHIMPORT:
            {
                if (secondToken.Type != KickAssemblerLexer.WS)
                {
                    return (false, -1);
                }
                int start = secondToken.Column + secondToken.Length();
                int end = doubleQuoteIndex - 1;
                if (end > start)
                {
                    foreach (char c in line[start..end])
                    {
                        if (c is not (' ' or '\t'))
                        {
                            return (false, -1);
                        }
                    }
                }

                break;
            }
            case KickAssemblerLexer.HASHIMPORTIF:
                if (secondToken.Type != KickAssemblerLexer.IIF_CONDITION)
                {
                    return (false, -1);
                }
                break;
        }
        return (true, doubleQuoteIndex);
    }
}