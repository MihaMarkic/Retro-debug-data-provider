using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static partial class QuotedCompletionOptions
{
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> tokens,
        string text, int lineStart, int lineLength, TextChangeTrigger trigger, int column)
    {
        var line = text.AsSpan()[lineStart..(lineStart + lineLength)];
        if (line.Length == 0)
        {
            return null;
        }

        // TODO properly handle valuesCountSupport (to limit it to single value when required)
        var cursorWithinArray = IsCursorWithinNonArray(text, lineStart, lineLength, column);
        if (cursorWithinArray is not null)
        {
            CompletionOptionType? completionOptionType = cursorWithinArray.Value.KeyWord switch
            {
                ".import" when cursorWithinArray.Value.Parameter is "c64" => CompletionOptionType.ProgramFile,
                ".import" when cursorWithinArray.Value.Parameter is "text" => CompletionOptionType.TextFile,
                ".import" when cursorWithinArray.Value.Parameter is "binary" => CompletionOptionType.BinaryFile,
                _ => null,
            };
            if (completionOptionType is not null)
            {
                var excludedValues = new string[] { cursorWithinArray.Value.CurrentValue }.ToFrozenSet();
                CompletionOption? completionOption = completionOptionType switch
                {
                    CompletionOptionType.ProgramFile or CompletionOptionType.BinaryFile or CompletionOptionType.TextFile
                        =>
                        new CompletionOption(completionOptionType.Value, cursorWithinArray.Value.Root,
                            cursorWithinArray.Value.HasEndDelimiter, cursorWithinArray.Value.ReplacementLength, 
                            excludedValues),
                    _ => null,
                };
                return completionOption;
            }
        }

        return null;
    }

    [GeneratedRegex("""
                    (?<KeyWord>(\.import))\s*(?<Parameter>\w*)?\s*(?<StartDoubleQuote>")\s*(?<Root>[^"]+)?$
                    """, RegexOptions.Singleline)]
    private static partial Regex NonArraySuggestionTemplateRegex();

    internal static IsCursorWithinNonArrayResult? IsCursorWithinNonArray(string text, int lineStart, int lineLength,
        int cursor)
    {
        Debug.WriteLine($"Searching IsCursorWithinNonArray in: '{text.Substring(lineStart, cursor + 1)}'");
        int lineEnd = lineStart + lineLength;
        // tries to match against text left of cursor
        var match = NonArraySuggestionTemplateRegex().Match(text, lineStart, cursor + 1);
        if (match.Success)
        {
            var line = text.AsSpan()[lineStart..lineEnd];
            var rootGroup = match.Groups["Root"];
            int startDoubleQuote = match.Groups["StartDoubleQuote"].Index - lineStart;
            ReadOnlySpan<char> currentValue;
            int endDoubleQuote;
            if (line.Length > startDoubleQuote)
            {
                int firstCharAfterQuotes = startDoubleQuote + 1;
                endDoubleQuote = line.Length > startDoubleQuote ? line[firstCharAfterQuotes..].IndexOf('"') : -1;
                currentValue = endDoubleQuote < 0
                    ? line[firstCharAfterQuotes..]
                    : line.Slice(firstCharAfterQuotes, endDoubleQuote);
            }
            else
            {
                currentValue = [];
                endDoubleQuote = 0;
            }

            Debug.WriteLine($"Found a match with current being '{currentValue}'");

            return new IsCursorWithinNonArrayResult(
                match.Groups["KeyWord"].Value,
                match.Groups["Parameter"].Value,
                rootGroup.Value,
                currentValue.ToString(),
                ReplacementLength: currentValue.Length,
                HasEndDelimiter: endDoubleQuote >= 0
            );
        }
        else
        {
            Debug.WriteLine("Doesn't match");
        }

        return null;
    }

    internal record struct IsCursorWithinNonArrayResult(
        string KeyWord,
        string? Parameter,
        string Root,
        string CurrentValue,
        int ReplacementLength,
        bool HasEndDelimiter);
}