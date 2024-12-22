using Antlr4.Runtime;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class CompletionOptionCollectorsCommon
{
    /// <summary>
    /// Extracts text of left caret, entire replaceable length and whether it ends with double quote or not
    /// </summary>
    /// <param name="line">Text right to first double quote</param>
    /// <param name="caret">Caret position within line</param>
    /// <returns></returns>
    internal static (string RootText, int Length, bool EndsWithDoubleQuote) GetSuggestionTextInDoubleQuotes(
        ReadOnlySpan<char> line, int caret)
    {
        if (caret > line.Length || caret < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(caret));
        }
        string root = line[..caret].ToString();
        int indexOfDoubleQuote = line.IndexOf('"');
        if (indexOfDoubleQuote < 0)
        {
            return (root, line.Length, false);
        }
        return (root, indexOfDoubleQuote, true);
    }
    

    /// <summary>
    /// Trims WS tokens at start and both WS and EOL at the end of the line. 
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    internal static ReadOnlySpan<IToken> TrimWhitespaces(ReadOnlySpan<IToken> tokens)
    {
        int start = 0;
        while (start < tokens.Length && (tokens[start].Type == KickAssemblerLexer.WS || tokens[start].Channel != 0))
        {
            start++;
        }

        if (start == tokens.Length)
        {
            return ReadOnlySpan<IToken>.Empty;
        }
        int end = tokens.Length - 1;
        while (end >= start &&
               tokens[end].Type is KickAssemblerLexer.WS or KickAssemblerLexer.EOL or KickAssemblerLexer.Eof)
        {
            end--;
        }

        return tokens[start..(end+1)];
    }
}