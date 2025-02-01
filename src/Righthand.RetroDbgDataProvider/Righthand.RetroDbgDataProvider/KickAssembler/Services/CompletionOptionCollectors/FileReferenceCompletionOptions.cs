using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class FileReferenceCompletionOptions
{
    /// <summary>
    /// Returns possible completion for file references.
    /// </summary>
    /// <param name="lineTokens"></param>
    /// <param name="line"></param>
    /// <param name="trigger"></param>
    /// <param name="column"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> lineTokens,
        ReadOnlySpan<char> line, TextChangeTrigger trigger, int column, CompletionOptionContext context)
    {
        Debug.WriteLine($"Trying {nameof(FileReferenceCompletionOptions)}");
        var leftLinePart = line[..column];
        var (isMatch, doubleQuoteColumn) = GetFileReferenceSuggestion(lineTokens, leftLinePart, trigger);
        if (isMatch)
        {
            var suggestionLine = line[(doubleQuoteColumn + 1)..];
            var (rootText, length, endsWithDoubleQuote) = suggestionLine.GetSuggestionTextInDoubleQuotes(column - doubleQuoteColumn - 1);
            FrozenSet<string> excluded = [suggestionLine.Slice(0, length).ToString()];
            FrozenSet<string> fileExtensions = [".asm"];
            var suggestions = CompletionOptionCollectorsCommon.CollectFileSystemSuggestions(rootText, fileExtensions, excluded, context.ProjectServices);
            return new CompletionOption(rootText, length, string.Empty, endsWithDoubleQuote ? "" : "\"", suggestions);
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
                if (secondToken.Type is not (STRING or OPEN_STRING))
                {
                    return (false, -1);
                }
                // if (secondToken.Type != KickAssemblerLexer.WS)
                // {
                //     return (false, -1);
                // }
                // int start = secondToken.Column + secondToken.Length();
                // int end = doubleQuoteIndex - 1;
                // if (end > start)
                // {
                //     foreach (char c in line[start..end])
                //     {
                //         if (c is not (' ' or '\t'))
                //         {
                //             return (false, -1);
                //         }
                //     }
                // }

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