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
    
    /// <summary>
    /// Checks whether suggestions happens within string or comment.
    /// </summary>
    /// <param name="text">Text before suggestions match</param>
    /// <returns>True when prefix is not a string or comment, false otherwise.</returns>
    internal static bool IsPrefixValidForSuggestions(ReadOnlySpan<char> text)
    {
        IsPrefixValidForSuggestionsStatus status =  IsPrefixValidForSuggestionsStatus.None;
        foreach (char c in text)
        {
            switch (c)
            {
                case '\"':
                    status = status switch
                    {
                        IsPrefixValidForSuggestionsStatus.String => IsPrefixValidForSuggestionsStatus.None,
                        _ => IsPrefixValidForSuggestionsStatus.String,
                    };
                    break;
                case '/':
                    switch (status)
                    {
                        case IsPrefixValidForSuggestionsStatus.FirstCommentChar:
                            return false;
                        case IsPrefixValidForSuggestionsStatus.String:
                            break;
                        default:
                            status = IsPrefixValidForSuggestionsStatus.FirstCommentChar;
                            break;
                    }
                    break;
            }
        }

        return status != IsPrefixValidForSuggestionsStatus.String;
    }

    public enum IsPrefixValidForSuggestionsStatus
    {
        None,
        String,
        FirstCommentChar
    }

}