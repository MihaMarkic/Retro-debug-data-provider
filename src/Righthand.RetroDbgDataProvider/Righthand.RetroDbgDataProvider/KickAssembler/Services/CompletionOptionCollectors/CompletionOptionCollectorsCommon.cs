using System.Collections.Frozen;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using Righthand.RetroDbgDataProvider.Services.Implementation;

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
    internal static FrozenSet<Suggestion> CreateSuggestionsFromTexts(string root, FrozenSet<string> suggestionTexts, SuggestionOrigin origin)
    {
        return suggestionTexts.Count > 0
            ? suggestionTexts
                .Where(t => t.StartsWith(root, StringComparison.OrdinalIgnoreCase) && !t.Equals(root, StringComparison.OrdinalIgnoreCase))
                .Select(t => new StandardSuggestion(origin, t))
                .Cast<Suggestion>()
                .ToFrozenSet() 
            : [];
    }
    

    internal static FrozenSet<string> CollectSegmentsSuggestions(string rootText, FrozenSet<string> excluded, IProjectServices projectServices)
    {
        var result = projectServices.CollectSegments()
            .Where(s => !excluded.Contains(s) && s.StartsWith(rootText, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToFrozenSet();
        return result;
    }
    /// <summary>
    /// Collects both files and directories that match input arguments.
    /// Returned suggestions always contain non windows delimited paths.
    /// </summary>
    /// <param name="relativeFilePath"></param>
    /// <param name="rootText"></param>
    /// <param name="fileExtensions"></param>
    /// <param name="excluded"></param>
    /// <param name="projectServices"></param>
    /// <returns></returns>
    internal static FrozenSet<Suggestion> CollectFileSystemSuggestions(string relativeFilePath, string rootText, FrozenSet<string> fileExtensions, FrozenSet<string> excluded, IProjectServices projectServices)
    {
        var builder = new HashSet<Suggestion>();
        var normalizedRootText = OsDependent.NormalizePath(rootText);
        var files = projectServices.GetMatchingFiles(relativeFilePath, normalizedRootText, fileExtensions, excluded);
        foreach (var p in files)
        {
            foreach (var f in p.Value)
            {
                builder.Add(new FileSuggestion(f.ToNonWindowsPath(), p.Key.Origin, p.Key.Path));
            }
        }
        var directories = projectServices.GetMatchingDirectories(relativeFilePath, normalizedRootText);
        foreach (var p in directories)
        {
            foreach (var d in p.Value)
            {
                builder.Add(new DirectorySuggestion(d.ToNonWindowsPath(), p.Key.Origin, p.Key.Path));
            }
        }

        return builder.ToFrozenSet();
    }

    internal static string ToNonWindowsPath(this string path) => path.Replace(@"\", "/");

    /// <summary>
    /// Extracts text of left caret, entire replaceable length and whether it ends with double quote or not
    /// </summary>
    /// <param name="line">Text right to first double quote</param>
    /// <param name="caret">Caret position within line</param>
    /// <returns></returns>
    internal static (string RootText, int Length, bool EndsWithDoubleQuote) GetSuggestionTextInDoubleQuotes(this ReadOnlySpan<char> line, int caret)
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