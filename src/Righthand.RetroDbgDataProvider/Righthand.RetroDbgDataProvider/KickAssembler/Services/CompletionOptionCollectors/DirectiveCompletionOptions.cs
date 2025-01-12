using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static partial class DirectiveCompletionOptions
{
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int column, CompletionOptionContext context)
    {
        var line = text.AsSpan()[lineStart..(lineStart + lineLength)];
        if (line.Length == 0)
        {
            return null;
        }

        var cursorWithinValue = GetMetaInformation(text, lineStart, lineLength, column);
        if (cursorWithinValue is not null)
        {
            var (position, keyword, parameter, root, currentValue, replacementLength, hasEndDelimiter) = cursorWithinValue.Value;
            switch (position)
            {
                case PositionType.Value:
                    var directiveValues = DirectiveProperties.GetValueTypes(keyword, parameter);
                    if (directiveValues is not null)
                    {
                        var fileExtensions = directiveValues.Items.OfType<FileDirectiveValueType>()
                            .Select(vt => vt.FileExtension)
                            .ToFrozenSet();
                        if (fileExtensions.Any())
                        {
                            FrozenSet<string> excluded = [currentValue];
                            if (fileExtensions.Count > 0)
                            {
                                var suggestions = CompletionOptionCollectorsCommon.CollectFileSystemSuggestions(root, fileExtensions, excluded, context.ProjectServices);
                                return new CompletionOption(root, replacementLength, string.Empty, hasEndDelimiter ? "" : "\"", suggestions.ToFrozenSet());
                            }
                        }
                        else
                        {
                            var enumerationValues = directiveValues.Items.OfType<EnumerableDirectiveValueType>().ToFrozenSet();
                            if (enumerationValues.Any())
                            {
                                var suggestions = enumerationValues
                                    .Where(v => v.Value.StartsWith(root))
                                    .Select(v => new StandardSuggestion(SuggestionOrigin.DirectiveOption, v.Value))
                                    .Cast<Suggestion>()
                                    .ToFrozenSet();
                                return new CompletionOption(root, replacementLength, string.Empty, hasEndDelimiter ? "" : "\"", suggestions.ToFrozenSet());
                            }
                        }
                    }

                    break;
                case PositionType.Type:
                    switch (keyword)
                    {
                        case ".import":
                            if (DirectiveProperties.TryGetDirective(".import", out var directive) && directive is DirectiveWithType directiveWithType)
                            {
                                var fileExtension = Path.GetExtension(currentValue);
                                var defaultSuggestion = fileExtension switch
                                {
                                    ".c64" => "c64",
                                    ".txt" => "txt",
                                    ".bin" => "bin",
                                    _ => null,
                                };

                                var suggestions =
                                    directiveWithType.ValueTypes.Keys
                                        .Where(k => k.StartsWith(root))
                                        .Select(k => new StandardSuggestion(SuggestionOrigin.DirectiveOption, k, 0) { IsDefault = k == defaultSuggestion })
                                        .Cast<Suggestion>()
                                        .ToFrozenSet();
                                return new CompletionOption(root, replacementLength, string.Empty, string.Empty, suggestions.ToFrozenSet());
                            }

                            break;
                    }

                    break;
            }
        }

        return null;
    }

    public enum PositionType
    {
        Type,
        Value,
    }

    #region tokenized approach

    // internal static (PositionType PositionType, string DirectiveName, int DirectiveType, string? Parameter, string? Root, string? CurrentFileValue)? 
    //     GetStatus(ReadOnlySpan<IToken> lineTokens, string text, int cursor)
    // {
    //     var cursorTokenIndex = TokenListOperations.GetTokenIndexAtColumn(lineTokens, 0, cursor);
    //     if (cursorTokenIndex is not null)
    //     {
    //         int index = cursorTokenIndex.Value;
    //         var currentToken = lineTokens[index];
    //         // first find candidate for start analyse
    //         while (index >= 0 && currentToken.Type is not (DOUBLE_QUOTE or STRING or UNQUOTED_STRING))
    //         {
    //             index--;
    //             currentToken = lineTokens[index];
    //         }
    //
    //         if (index < 0)
    //         {
    //             return null;
    //         }
    //
    //         // Unquoted string is problematic because it's not obvious whether belongs inside an open string or not
    //         if (currentToken.Type == UNQUOTED_STRING)
    //         {
    //             if (lineTokens[..index].GetLastIndexOf(DOUBLE_QUOTE, out var lastDoubleQuotesOnLeft))
    //             {
    //                 index = lastDoubleQuotesOnLeft.Value;
    //                 currentToken = lineTokens[index];
    //             }
    //         }
    //         switch (currentToken.Type)
    //         {
    //             case DOUBLE_QUOTE:
    //                 if (index > 1)
    //                 {
    //                     var previousToken = lineTokens[index - 1];
    //                     if (previousToken.Type == UNQUOTED_STRING)
    //                     {
    //                         var firstToken = lineTokens[index - 2];
    //                         if (firstToken.IsDirectiveType())
    //                         {
    //                             return (PositionType.Type, firstToken.Text, firstToken.Type, previousToken.Text, "", "");
    //                         }
    //                     }
    //                 }
    //                 break;
    //             case STRING:
    //                 if (index > 1)
    //                 {
    //                     var previousToken = lineTokens[index - 1];
    //                     if (previousToken.Type == UNQUOTED_STRING)
    //                     {
    //                         var firstToken = lineTokens[index - 2];
    //                         if (firstToken.IsDirectiveType())
    //                         {
    //                             var currentValue = currentToken.Text.Trim('\"');
    //                             string root = currentToken.Text[1..(cursor - currentToken.StartIndex)];
    //                             return (PositionType.Type, firstToken.Text, firstToken.Type, previousToken.Text, root, currentValue);
    //                         }
    //                     }
    //                 }
    //                 break;
    //             case UNQUOTED_STRING:
    //                 if (index > 0)
    //                 {
    //                     var previousToken = lineTokens[index - 1];
    //                     if (previousToken.IsDirectiveType())
    //                     {
    //                         string? currentValue = null;
    //                         if (index < lineTokens.Length - 1)
    //                         {
    //                             var nextToken = lineTokens[index + 1];
    //                             switch (nextToken.Type)
    //                             {
    //                                 case STRING:
    //                                     currentValue = nextToken.Text.Trim('\"');
    //                                     break;
    //                                 case DOUBLE_QUOTE:
    //                                     currentValue = ExtractOpenString(text, nextToken.StopIndex, lineTokens[^1].StopIndex);
    //                                     break;
    //                             }
    //                         }
    //
    //                         string root = currentToken.Text[0..(cursor - currentToken.StartIndex)];
    //                         return (PositionType.Type, previousToken.Text, previousToken.Type, currentToken.Text, root, currentValue);
    //                     }
    //                 }
    //
    //                 break;
    //         }
    //     }
    //     return null;
    // }
    //
    // internal static string? ExtractOpenString(string text, int startIndex, int endIndex)
    // {
    //     return text[startIndex..endIndex];
    // }
    
    

    // [GeneratedRegex("""
    //                 (?<KeyWord>(\.import))\s+(?<Parameter>\w*)?
    //                 """, RegexOptions.Singleline)]
    // private static partial Regex CursorOnDirectiveType();
    // [GeneratedRegex("""
    //                 ^(?<Parameter>\w*)?\s*(?<StartDoubleQuote>")(?<CurrentValue>[^"]+)?
    //                 """, RegexOptions.Singleline)]
    // private static partial Regex CursorOnFullDirectiveType();
    // internal static (string Keyword, string? Parameter, string? Root, string? CurrentFileValue)? IsCursorOnParameterType(string text, int lineStart, int lineLength,
    //     int cursor)
    // {
    //     var match = CursorOnDirectiveType().Match(text, lineStart, cursor + 1);
    //     if (match.Success)
    //     {
    //         int lineEnd = lineStart + lineLength;
    //         var keyword = match.Groups["KeyWord"].Value;
    //         string? root = null;
    //         var parameterGroup = match.Groups["Parameter"];
    //         int parameterStart = cursor;
    //         if (parameterGroup is not null)
    //         {
    //             root = parameterGroup.Value;
    //             parameterStart = cursor - root.Length;
    //         }
    //         var fullMatch = CursorOnFullDirectiveType().Match(text, parameterStart, lineEnd-parameterStart);
    //         var parameter = fullMatch.Groups["Parameter"].Value;
    //         var currentValue = fullMatch.Groups["CurrentValue"].Value;
    //         return (keyword, parameter, root, currentValue);
    //     }
    //
    //     return null;
    // }
#endregion
    [GeneratedRegex("""
                    (?<FullKeyWord>\.(?<KeyWord>([a-zA-Z]+)))(?<ParameterSpace>\s+(?<Parameter>\w*)\s*)?(?<Value>(?<StartDoubleQuote>")(?<CurrentValue>[^"]+)?(?<EndDoubleQuote>")?)?
                    """, RegexOptions.Singleline)]
    private static partial Regex QuotedValueTemplateRegex();

    internal static LineMeta? GetMetaInformation(string text, int lineStart, int lineLength,
        int lineCursor)
    {
        Debug.WriteLine($"Searching IsCursorWithinNonArray in: '{text.Substring(lineStart, lineLength)}'");
        int lineEnd = lineStart + lineLength;
        int absoluteCursor = lineStart + lineCursor;
        // tries to match against text left of cursor
        int matchStart = lineStart;
        Match match;
        do
        {
            match = QuotedValueTemplateRegex().Match(text, matchStart, lineEnd-matchStart);
            matchStart = match.Index + 1;
            
        } while (match.Success && !(match.Index <= absoluteCursor && match.Index + match.Length >= absoluteCursor));
        if (match.Success)
        {
            var line = text.AsSpan()[lineStart..lineEnd];
            var parameterSpaceGroup = match.Groups["ParameterSpace"];
            var currentValueGroup = match.Groups["CurrentValue"];
            bool hasEndDelimiter;
            var valueGroup = match.Groups["Value"];
            ReadOnlySpan<char> currentValue;
            PositionType positionType;
            string root;
            int replacementLength;
            if (parameterSpaceGroup.IsWithin(absoluteCursor))
            {
                positionType = PositionType.Type;
                currentValue = currentValueGroup?.Value;
                var length = absoluteCursor - parameterSpaceGroup.Index + 1;
                root = parameterSpaceGroup.Value[..length].TrimStart();
                var parameterLength = match.Groups["Parameter"]?.Value.Length ?? 0;
                replacementLength = parameterLength;
                hasEndDelimiter = false;
            }
            else if (currentValueGroup.IsWithin(absoluteCursor))
            {
                positionType = PositionType.Value;
                currentValue = currentValueGroup.Value;
                var length = absoluteCursor - currentValueGroup.Index+1;
                root = currentValueGroup.Value[..length];
                replacementLength = currentValue.Length;
                hasEndDelimiter =  match.Groups["EndDoubleQuote"].Success;
            }
            else if (valueGroup.IsWithin(absoluteCursor))
            {
                positionType = PositionType.Value;
                currentValue = "";
                replacementLength = 0;
                root = "";
                hasEndDelimiter =  match.Groups["EndDoubleQuote"].Success;
            }
            else
            {
                return null;
            }

            Debug.WriteLine($"Found a match with current being '{currentValue}'");

            return new LineMeta(
                positionType,
                match.Groups["FullKeyWord"].Value,
                match.Groups["Parameter"].Value,
                root,
                currentValue.ToString(),
                ReplacementLength: replacementLength,
                HasEndDelimiter: hasEndDelimiter
            );
        }
        else
        {
            Debug.WriteLine("Doesn't match");
        }

        return null;
    }

    internal record struct LineMeta(
        PositionType PositionType,
        string Directive,
        string? Parameter,
        string Root,
        string CurrentValue,
        int ReplacementLength,
        bool HasEndDelimiter);
}