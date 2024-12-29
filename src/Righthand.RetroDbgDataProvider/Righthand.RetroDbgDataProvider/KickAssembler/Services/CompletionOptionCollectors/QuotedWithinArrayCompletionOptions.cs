using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static partial class QuotedWithinArrayCompletionOptions
{
    /// <summary>
    /// Generic file suggestion completion.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="text"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineLength"></param>
    /// <param name="trigger"></param>
    /// <param name="column"></param>
    /// <param name="valuesCountSupport"></param>
    /// <returns></returns>
    /// <remarks>
    /// Handles such cases:
    /// .file [name="test.prg", segments="Code", sidFiles="file.sid"]
    /// .segment Base [prgFiles="basefile.prg"]
    /// .segmentdef Misc1 [prgFiles="data/Music.prg, data/Charset2x2.prg"]
    /// *** .import c64 "data/Music.prg"
    /// .segment Main [sidFiles="data/music.sid", outPrg="out.prg"]
    ///
    /// Where files = "file(, file)*"
    /// </remarks>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> tokens,
        string text, int lineStart, int lineLength, TextChangeTrigger trigger, int column, KickAssemblerParsedSourceFile.ValuesCount valuesCountSupport)
    {
        var line = text.AsSpan()[lineStart..(lineStart + lineLength)];
        if (line.Length == 0)
        {
            return null;
        }

        // TODO properly handle valuesCountSupport (to limit it to single value when required)
        var cursorWithinArray = IsCursorWithinArray(text, lineStart, lineLength, column, valuesCountSupport);
        // if (cursorWithinArray is not null)
        // {
        //     CompletionOptionType? completionOptionType = cursorWithinArray.Value.ArgumentName switch
        //     {
        //         "sidFiles" => CompletionOptionType.SidFile,
        //         "prgFiles" or "name" => CompletionOptionType.ProgramFile,
        //         "segments" => CompletionOptionType.Segments,
        //         _ => null,
        //     };
        //     if (completionOptionType is not null)
        //     {
                // CompletionOption? completionOption = completionOptionType switch
                // {
                //     CompletionOptionType.SidFile when cursorWithinArray.Value.KeyWord is ".segment" or ".segmentdef"
                //             or ".segmentout"
                //             or "file" =>
                //         new CompletionOption(completionOptionType.Value, cursorWithinArray.Value.Root,
                //             cursorWithinArray.Value.HasEndDelimiter, cursorWithinArray.Value.ReplacementLength,
                //             cursorWithinArray.Value.ArrayValues.Distinct().ToFrozenSet()),
                //     CompletionOptionType.ProgramFile when cursorWithinArray.Value.KeyWord is ".segment" or ".segmentdef"
                //             or ".segmentout"
                //         =>
                //         new CompletionOption(completionOptionType.Value, cursorWithinArray.Value.Root,
                //             cursorWithinArray.Value.HasEndDelimiter, cursorWithinArray.Value.ReplacementLength,
                //             cursorWithinArray.Value.ArrayValues.Distinct().ToFrozenSet()),
                //     CompletionOptionType.Segments when cursorWithinArray.Value.KeyWord is ".file" or ".segmentdef"
                //         or ".segmentout" => GetCompletionOptionForSegments(completionOptionType.Value,
                //         cursorWithinArray.Value),
                //
                //     _ => null,
                // };
                // return completionOption;
        //     }
        // }

        return null;
    }
    // ReSharper disable once StringLiteralTypo
    [GeneratedRegex("""
                    (?<KeyWord>(\.segmentdef|\.segment|.segmentout|\.file))\s*(?<Parameter>\w+)?\s*(?<OpenBracket>\[)\s*((?<PrevArgName>\w+)\s*(=\s*((?<PrevQuotedValue>".*")|(?<PrevUnquotedValue>[^,\s]+)))?\s*,\s*)*(?<ArgName>\w+)\s*=\s*(?<StartDoubleQuote>")(\s*(?<PrevArrayItem>[^,"]*)\s*(?<ArgComma>,))*\s*(?<Root>[^,"]*)$
                    """, RegexOptions.Singleline)]
    private static partial Regex ArraySuggestionTemplateRegex();
    /// <summary>
    /// Finds whether cursor is within array of options. KeyWord is limited to .segmentDef, .segment and .file
    /// </summary>
    /// <param name="text"></param>
    /// <param name="lineStart">Line start in absolute index</param>
    /// <param name="lineLength"></param>
    /// <param name="cursor">Cursor position in absolute index</param>
    /// <param name="valuesCountSupport">When Multiple, multiple comma-delimited values are supported, only a single value otherwise</param>
    /// <returns></returns>
    /// <remarks>
    /// Groups:
    /// - StartDoubleQuote is starting double quote of values
    /// </remarks>
    internal static IsCursorWithinArrayResult? IsCursorWithinArray(string text, int lineStart, int lineLength,
        int cursor, KickAssemblerParsedSourceFile.ValuesCount valuesCountSupport)
    { 
        Debug.WriteLine($"Searching IsCursorWithinArrayResult in: '{text.Substring(lineStart, cursor+1)}'");
        int lineEnd = lineStart + lineLength;
        // tries to match against text left of cursor
        var match = ArraySuggestionTemplateRegex().Match(text, lineStart, cursor+1);
        if (match.Success)
        {
            // when supports only a single value, can't have comma-separated values in front 
            if (valuesCountSupport == KickAssemblerParsedSourceFile.ValuesCount.Single && match.Groups["PrevArrayItem"].Success)
            {
                Debug.WriteLine("Doesn't support multiple values and comma was found");
                return null;
            }

            var line = text.AsSpan()[lineStart..lineEnd];
            int? firstDelimiterColumn = FindFirstArrayDelimiterPosition(line, cursor+1) + lineStart;
            var rootGroup = match.Groups["Root"];
            int startDoubleQuote = match.Groups["StartDoubleQuote"].Index;
            var arrayValues = GetArrayValues(text, startDoubleQuote, lineEnd - startDoubleQuote);
            var currentValue = GetCurrentArrayValue(text, rootGroup.Index, lineEnd);
            Debug.WriteLine($"Found a match with current being '{currentValue}' and array values {string.Join(",", arrayValues.Select(a => $"'{a}'"))}");
            
            return new IsCursorWithinArrayResult(
                match.Groups["KeyWord"].Value,
                match.Groups["Parameter"].Value,
                match.Groups["ArgName"].Value,
                rootGroup.Value,
                match.Groups["OpenBracket"].Index,
                ReplacementLength: currentValue.Length,
                HasEndDelimiter: firstDelimiterColumn is not null,
                arrayValues
            );
        }
        else
        {
            Debug.WriteLine("Doesn't match");
        }

        return null;
    }
    

    [GeneratedRegex("""
                    ^\s*(?<Item>[^,"]*)
                    """, RegexOptions.Singleline)]
    private static partial Regex GetCurrentArrayValueRegex();
    internal static string GetCurrentArrayValue(string text, int start, int end)
    {
        var match = GetCurrentArrayValueRegex().Match(text, start, end-start);
        if (match.Success)
        {
            return match.Groups["Item"].Value;
        }

        throw new Exception("Shouldn't happen");
    }

    [GeneratedRegex("""
                    ^"(\s*(?<ArrayItem>[^,"]*)\s*,)*\s*(?<LastItem>[^,"]*)"?
                    """, RegexOptions.Singleline)]
    private static partial Regex GetArrayValuesRegex();
    internal static ImmutableArray<string> GetArrayValues(string text, int start, int length)
    {
        var m = GetArrayValuesRegex().Match(text, start, length);
        if (m.Success)
        {
            var items = m.Groups["ArrayItem"].Captures
                .Where(c => !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => c.Value)
                .ToImmutableArray();
            string? lastItem = m.Groups["LastItem"].Value;
            if (!string.IsNullOrWhiteSpace(lastItem))
            {
                return items.Add(lastItem);
            }

            return items;
        }

        return [];
    }

    internal static int? FindFirstArrayDelimiterPosition(ReadOnlySpan<char> line, int cursor)
    {
        int delimiterPosition = cursor;
        while (delimiterPosition < line.Length)
        {
            if (line[delimiterPosition] is ',' or '"')
            {
                return delimiterPosition;
            }
            delimiterPosition++;
        }

        return null;
    }

    // private static CompletionOption GetCompletionOptionForSegments(CompletionOptionType completionOptionType,
    //     IsCursorWithinArrayResult data)
    // {
    //     ImmutableArray<string> excludedValues;
    //     if (data.KeyWord.Equals(".segmentdef", StringComparison.Ordinal) && data.Parameter is not null)
    //     {
    //         excludedValues = data.ArrayValues.Add(data.Parameter);
    //     }
    //     else
    //     {
    //         excludedValues = data.ArrayValues;
    //     }
    //
    //     return new CompletionOption(completionOptionType, data.Root, data.HasEndDelimiter, data.ReplacementLength,
    //         excludedValues.Distinct(OsDependent.FileStringComparer).ToFrozenSet());
    // }
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    internal record struct IsCursorWithinArrayResult(
        string KeyWord,
        string? Parameter,
        string ArgumentName,
        string Root,
        int OpenBracketColumn,
        int ReplacementLength,
        bool HasEndDelimiter,
        ImmutableArray<string> ArrayValues);
}