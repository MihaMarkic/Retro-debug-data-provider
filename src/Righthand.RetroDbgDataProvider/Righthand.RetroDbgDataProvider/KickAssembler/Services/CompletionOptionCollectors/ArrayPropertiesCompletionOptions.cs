using System.Diagnostics;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static partial class ArrayPropertiesCompletionOptions
{
    [GeneratedRegex("""
                    (?<KeyWord>(.file))\s*(?<Parameter>\w+)?\s*(?<OpenBracket>\[)\s*((?<PrevArgName>\w+)\s*(=\s*((?<PrevQuotedValue>".*")|(?<PrevUnquotedValue>[^,\s]+)))?\s*,)*(?<Root>\w*)$/s
                    """, RegexOptions.Singleline)]
    private static partial Regex ArrayKeywordSuggestionTemplateRegex();

    internal static IsCursorWithinArrayKeywordResult? IsCursorWithinArrayKeyword(string text, int lineStart,
        int lineLength,
        int cursor)
    {
        Debug.WriteLine($"Searching IsCursorWithinArrayKeyword in: '{text.Substring(lineStart, cursor + 1)}'");
        int lineEnd = lineStart + lineLength;
        // tries to match against text left of cursor
        var match = ArrayKeywordSuggestionTemplateRegex().Match(text, lineStart, cursor + 1);
        if (match.Success)
        {
            var line = text.AsSpan()[lineStart..lineEnd];
            int matchIndexWithinLine = match.Index - lineStart;
            if (matchIndexWithinLine > 0)
            {
                var matchPrefix = line[..matchIndexWithinLine];
                if (!CompletionOptionCollectorsCommon.IsPrefixValidForSuggestions(matchPrefix))
                {
                    return null;
                }
            }

            var rootGroup = match.Groups["Root"];
            int rootEndInLine = rootGroup.Index - lineStart + rootGroup.Length;
            ReadOnlySpan<char> currentValue;
            if (line.Length > rootEndInLine)
            {
                var lineSuffix = line[rootEndInLine..];
                var end = Math.Min(lineSuffix.IndexOf(','), lineSuffix.IndexOf(']'));
                currentValue = end < 0 ? lineSuffix : lineSuffix[..end];
            }
            else
            {
                currentValue = [];
            }

            Debug.WriteLine($"Found a match with current being '{currentValue}'");

            return new IsCursorWithinArrayKeywordResult(
                match.Groups["KeyWord"].Value,
                match.Groups["Parameter"].Value,
                rootGroup.Value,
                ReplacementLength: currentValue.Length
            );
        }
        else
        {
            Debug.WriteLine("Doesn't match");
        }

        return null;
    }

    [GeneratedRegex("""

                    """)]
    private static partial Regex ArrayPropertiesRegex();

    internal static ImmutableArray<string> CollectArrayProperties(ReadOnlySpan<char> array)
    {
        return ImmutableArray<string>.Empty;
    }

    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> tokens,
        string text, int lineStart, int lineLength, TextChangeTrigger trigger, int column)
    {
        var line = text.AsSpan()[lineStart..(lineStart + lineLength)];
        if (line.Length == 0)
        {
            return null;
        }

        // TODO properly handle valuesCountSupport (to limit it to single value when required)
        var cursorWithinArrayKeyword = IsCursorWithinArrayKeyword(text, lineStart, lineLength, column);
        if (cursorWithinArrayKeyword is not null)
        {
            return new CompletionOption(CompletionOptionType.ArrayProperty, cursorWithinArrayKeyword.Value.Root,
                false, cursorWithinArrayKeyword.Value.ReplacementLength, []);
        }

        return null;
    }

    internal record struct IsCursorWithinArrayKeywordResult(
        string KeyWord,
        string? Parameter,
        string Root,
        int ReplacementLength);
}