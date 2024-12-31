using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static partial class ArrayPropertiesCompletionOptions
{
    [GeneratedRegex("""
                    (?<KeyWord>(.file))\s*(?<Parameter>\w+)?\s*(?<OpenBracket>\[)(\s*(?<PrevArgName>\w+)\s*(=\s*((?<PrevQuotedValue>".*")|(?<PrevUnquotedValue>[^,\s]+)))?\s*,)*\s*(?<Root>\w*)$
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

            int keyValueTextStart = match.Groups["OpenBracket"].Index + 1;
            var keyValueText = line[keyValueTextStart..];
            var existingProperties = ArrayContentExtractor.Extract(keyValueText)
                .Select(p => p.Key).ToImmutableArray();

            Debug.WriteLine($"Found a match with current being '{currentValue}'");

            return new IsCursorWithinArrayKeywordResult(
                match.Groups["KeyWord"].Value,
                match.Groups["Parameter"].Value,
                rootGroup.Value,
                ReplacementLength: currentValue.Length,
                existingProperties
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

    internal static CompletionOption? GetOption(string text, int lineStart, int lineLength, int column)
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
            string root = cursorWithinArrayKeyword.Value.Root;
            string keyWord = cursorWithinArrayKeyword.Value.KeyWord;
            var existingProperties = cursorWithinArrayKeyword.Value.ExistingProperties.Distinct().ToFrozenSet();
            var names = ArrayProperties.GetNames(keyWord, root);
            var query = names.Except(existingProperties);
            FrozenSet<Suggestion> suggestions =  [..names.Select(n => new Suggestion(SuggestionOrigin.PropertyName, n))];
                
            return new CompletionOption(cursorWithinArrayKeyword.Value.Root, cursorWithinArrayKeyword.Value.ReplacementLength, "", suggestions);
            // return new CompletionOption(CompletionOptionType.ArrayPropertyName, cursorWithinArrayKeyword.Value.Root,
            //     false, cursorWithinArrayKeyword.Value.ReplacementLength, existingProperties,
            //     cursorWithinArrayKeyword.Value.KeyWord);
        }

        return null;
    }

    internal record struct IsCursorWithinArrayKeywordResult(
        string KeyWord,
        string? Parameter,
        string Root,
        int ReplacementLength,
        ImmutableArray<string> ExistingProperties);
}