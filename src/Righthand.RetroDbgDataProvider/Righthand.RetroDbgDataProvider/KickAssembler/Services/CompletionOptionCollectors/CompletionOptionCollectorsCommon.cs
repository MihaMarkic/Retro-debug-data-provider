﻿using Antlr4.Runtime;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

[Flags]
public enum SyntaxStatus
{
    None = 0,
    Error = 1,
    String = 2,
    Comment = 4,
    Array = 8,
}

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
    /// Checks whether suggestions happens within string or comment and returns the status after line ending.
    /// </summary>
    /// <param name="text">Text before suggestions match</param>
    /// <returns>True when prefix is not a string or comment, false otherwise.</returns>
    internal static SyntaxStatus GetSyntaxStatusAtThenEnd(ReadOnlySpan<char> text)
    {
        IsPrefixValidForSuggestionsStatus status =  IsPrefixValidForSuggestionsStatus.None;
        bool isArray = false;
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
                            return SyntaxStatus.Comment | (isArray ? SyntaxStatus.Array : SyntaxStatus.None);
                        case IsPrefixValidForSuggestionsStatus.None:
                            status = IsPrefixValidForSuggestionsStatus.FirstCommentChar;
                            break;
                    }
                    break;
                case '[':
                    switch (status)
                    {
                        case IsPrefixValidForSuggestionsStatus.None:
                            if (isArray)
                            {
                                return SyntaxStatus.Error | SyntaxStatus.Array;
                            }
                            isArray = true;
                            break;
                        case IsPrefixValidForSuggestionsStatus.FirstCommentChar:
                            return SyntaxStatus.Error;
                    }
                    break;
                case ']':
                    switch (status)
                    {
                        case IsPrefixValidForSuggestionsStatus.None:
                            if (!isArray)
                            {
                                return SyntaxStatus.Error | SyntaxStatus.Array;
                            }

                            isArray = false;
                            break;
                        case IsPrefixValidForSuggestionsStatus.FirstCommentChar:
                            return SyntaxStatus.Error | SyntaxStatus.Array;
                    }

                    break;
            }
        }

        var result = status switch
        {
            IsPrefixValidForSuggestionsStatus.String => SyntaxStatus.String,
            _ => SyntaxStatus.None,
        };
        if (isArray)
        {
            result |= SyntaxStatus.Array;
        }
        return result;
    }

    public enum IsPrefixValidForSuggestionsStatus
    {
        None,
        String,
        FirstCommentChar
    }

}