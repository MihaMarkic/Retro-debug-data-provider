using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class ArrayCompletionOptions
{
    /// <summary>
    /// Get completion option for arrays within a body, such as files in .disk.
    /// </summary>
    /// <param name="tokens">All tokens</param>
    /// <param name="content">All text</param>
    /// <param name="lineStart"></param>
    /// <param name="lineLength"></param>
    /// <param name="column">Cursor position, -1 based</param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> tokens, string content, int lineStart, int lineLength,
        int column, CompletionOptionContext context)
    {
        Debug.WriteLine($"Trying {nameof(ArrayCompletionOptions)}");
        var columnTokenIndex = tokens.GetTokenIndexAtColumn(lineStart, column);
        if (columnTokenIndex is null)
        {
            return null;
        }

        var columnToken = tokens[columnTokenIndex.Value];
        int? openBracketIndex = columnToken.Type == KickAssemblerLexer.OPEN_BRACKET 
            ? columnTokenIndex.Value: tokens[..columnTokenIndex.Value].FindWithinArrayOpenBracket();
        if (openBracketIndex is null)
        {
            Debug.WriteLine("Couldn't find array open bracket");
            return null;
        }
        int leftOfArray;
        bool isWithinDirectiveBody;
        var bodyStartIndex = TokenListOperations.FindBodyStartForArrays(tokens[..(openBracketIndex.Value)]);
        if (bodyStartIndex is not null)
        {

            var statementBeforeArrayStartIndex = TokenListOperations.SkipArray(tokens[..bodyStartIndex.Value], isMandatory: false);
            if (statementBeforeArrayStartIndex is null || statementBeforeArrayStartIndex.Value < 0)
            {
                Debug.WriteLine("Couldn't find statement array start");
                return null;
            }
            leftOfArray = statementBeforeArrayStartIndex.Value;
            isWithinDirectiveBody = true;
        }
        else
        {
            leftOfArray = openBracketIndex.Value - 1;
            isWithinDirectiveBody = false;
        }

        var statement = TokenListOperations.FindDirectiveAndOption(tokens[..(leftOfArray + 1)]);
        if (statement is null)
        {
            Debug.WriteLine("Couldn't find statement directive and option");
            return null;
        }

        string arrayOwner;
        if (isWithinDirectiveBody)
        {
            if (statement.Value.DirectiveToken.Type != KickAssemblerLexer.DISK)
            {
                Debug.WriteLine("Body arrays currently only supported with .disk directive");
                return null;
            }
            arrayOwner = ".DISK_FILE";
        }
        else
        {
            var directiveToken = statement.Value.DirectiveToken;
            arrayOwner = directiveToken.Text;
        }

        var arrayProperties = tokens[(openBracketIndex.Value + 1)..].GetArrayProperties();
        int absoluteColumn = lineStart + column;
        var contentUpToLineEnd = content.AsSpan()[..(lineStart+lineLength)];
        var (name, position, root, value, matchingArrayProperty) = arrayProperties.GetColumnPositionData(contentUpToLineEnd, absoluteColumn);

        switch (position)
        {
            case PositionWithinArray.Name:
                var replacementLength = name?.StopIndex - absoluteColumn + 1 ?? 0;
                var excluded = arrayProperties.Select(a => a.Key.Text).Distinct().ToFrozenSet(StringComparer.OrdinalIgnoreCase);
                var suggestedValues = ArrayProperties
                    .GetNames(arrayOwner, root)
                    .Where(n => !excluded.Contains(n))
                    .Select(v => new StandardSuggestion(SuggestionOrigin.PropertyName, v))
                    .Cast<Suggestion>()
                    .ToFrozenSet();
                return new CompletionOption(root, replacementLength, string.Empty, string.Empty, suggestedValues);
            case PositionWithinArray.Value:
                if (ArrayProperties.GetProperty(arrayOwner, name!.Text, out var propertyMeta))
                {
                    return CreateSuggestionsForArrayValue(root, value, absoluteColumn, arrayOwner, statement.Value.OptionToken?.Text, matchingArrayProperty, propertyMeta, context);
                }

                break;
            default:
                throw new Exception($"Invalid position {position}");
        }

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="root"></param>
    /// <param name="value"></param>
    /// <param name="absoluteColumn"></param>
    /// <param name="arrayOwner"></param>
    /// <param name="option"></param>
    /// <param name="matchingProperty"></param>
    /// <param name="arrayProperty"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal static CompletionOption CreateSuggestionsForArrayValue(string root, string? value, int absoluteColumn, string arrayOwner, string? option,
        ArrayPropertyMeta? matchingProperty, ArrayProperty arrayProperty,
        CompletionOptionContext context)
    {
        int replacementLength = value is not null ? value.AsSpan().Trim('"').Length : 0;

        bool endsWithDoubleQuote = false;
        bool prependDoubleQuote = false;
        FrozenSet<string> suggestionTexts;
        FrozenSet<Suggestion> suggestions = [];
        switch (arrayProperty.Type)
        {
            case ArrayPropertyType.Bool:
                suggestionTexts = [.. ArrayPropertyValues.BoolValues];
                suggestions = CompletionOptionCollectorsCommon.CreateSuggestionsFromTexts(root, suggestionTexts, SuggestionOrigin.PropertyValue);
                break;
            case ArrayPropertyType.QuotedEnumerable:
            {
                if (arrayProperty is ValuesArrayProperty valuesProperty)
                {
                    suggestionTexts = valuesProperty.Values?.Select(v => $"\"{v}\"").ToFrozenSet() ?? [];
                    suggestions = CompletionOptionCollectorsCommon.CreateSuggestionsFromTexts(root, suggestionTexts, SuggestionOrigin.PropertyValue);
                }
            }

                break;
            case ArrayPropertyType.Enumerable:
            {
                if (arrayProperty is ValuesArrayProperty valuesProperty)
                {
                    suggestionTexts = valuesProperty.Values ?? [];
                    suggestions = CompletionOptionCollectorsCommon.CreateSuggestionsFromTexts(root, suggestionTexts, SuggestionOrigin.PropertyValue);
                }

                break;
            }

            case ArrayPropertyType.Segments:
            {
                if (matchingProperty?.StartValue is not null && value is not null)
                {
                    bool startsWithDoubleQuote = value.StartsWith('\"');
                    // shall not suggest when segments=|"...
                    bool isCursorAtStart = absoluteColumn <= matchingProperty.StartValue.StartIndex;
                    if (startsWithDoubleQuote && !isCursorAtStart)
                    {
                        var values = GetArrayValues(value);
                        var (valueRoot, valueTextAtCaret) = GetRootValue(values, absoluteColumn - matchingProperty.StartValue.StartIndex);
                        replacementLength = valueTextAtCaret.Length;
                        root = valueRoot;
                        var valueTexts = values.Select(v => v.Text).ToImmutableArray();
                        if (!string.IsNullOrWhiteSpace(option))
                        {
                            valueTexts = valueTexts.Add(option);
                        }

                        var excluded = valueTexts.Distinct().ToFrozenSet();
                        suggestionTexts = CompletionOptionCollectorsCommon.CollectSegmentsSuggestions(valueRoot, excluded, context.ProjectServices);
                        suggestions = CompletionOptionCollectorsCommon.CreateSuggestionsFromTexts(valueRoot, suggestionTexts, SuggestionOrigin.PropertyValue);
                        prependDoubleQuote = !startsWithDoubleQuote;
                    }
                }
                else
                {
                    // there is no value and no valueRoot
                    suggestionTexts = CompletionOptionCollectorsCommon.CollectSegmentsSuggestions("", [], context.ProjectServices);
                    suggestions = CompletionOptionCollectorsCommon.CreateSuggestionsFromTexts("", suggestionTexts, SuggestionOrigin.PropertyValue);
                    prependDoubleQuote = true;
                }

                break;
            }
            case ArrayPropertyType.FileNames:
            {
                FrozenSet<string> excluded = [..GetArrayValues(value).Select(v => v.Text)];
                var property = (FileArrayProperty)arrayProperty;
                prependDoubleQuote = !value?.StartsWith('\"') ?? true;
                suggestions = CompletionOptionCollectorsCommon.CollectFileSystemSuggestions(root.TrimStart('\"'), property.ValidExtensions, excluded, context.ProjectServices);
                break;
            }
            case ArrayPropertyType.FileName:
            {
                var excluded = !string.IsNullOrEmpty(value) ? new HashSet<string>([value.Trim('\"')]).ToFrozenSet(): [];
                prependDoubleQuote = !value?.StartsWith('\"') ?? true;
                endsWithDoubleQuote = !value?.EndsWith('\"') ?? true;
                var property = (FileArrayProperty)arrayProperty;
                suggestions = CompletionOptionCollectorsCommon.CollectFileSystemSuggestions(root.TrimStart('\"'), property.ValidExtensions, excluded, context.ProjectServices);
                break;
            }
        }

        return new CompletionOption(root, replacementLength, prependDoubleQuote ? "\"" : string.Empty, endsWithDoubleQuote ? "\"" : string.Empty, suggestions);
    }

    /// <summary>
    /// Gets active value and root value left of the <param name="relativeColumn"/>. Stops upon space, comma and close bracket.
    /// </summary>
    /// <param name="values"></param>
    /// <param name="relativeColumn"></param>
    /// <returns></returns>
    internal static (string Root, string Value) GetRootValue(ImmutableArray<(string Text, int StartIndex)> values, int relativeColumn)
    {
        foreach (var v in values)
        {
            bool isInsideValue = v.StartIndex <= relativeColumn && v.StartIndex + v.Text.Length >= relativeColumn; 
            if (isInsideValue)
            {
                int spaceIndex = v.Text.IndexOf(' ', StringComparison.Ordinal);
                int commaIndex = v.Text.IndexOf(',', StringComparison.Ordinal);
                int closeBracketIndex = v.Text.IndexOf(']', StringComparison.Ordinal);
                int endingIndex = GetMinPosition(v.Text.Length, spaceIndex, commaIndex, closeBracketIndex);
                var trimmedValue = v.Text[..endingIndex];
                
                return (trimmedValue[..(relativeColumn - v.StartIndex)], trimmedValue);
            }
        }

        return (string.Empty, String.Empty);
    }

    /// <summary>
    /// Gets minimum position of given <param name="values" /> starting with <param name="defaultEnding" />.
    /// Negative values are ignored.
    /// </summary>
    /// <param name="defaultEnding"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    internal static int GetMinPosition(int defaultEnding, params ReadOnlySpan<int> values)
    {
        var min = defaultEnding;
        foreach (var v in values)
        {
            if (v >= 0 && v < min)
            {
                min = v;
            }
        }
        return min;
    }

    internal static ImmutableArray<(string Text, int StartIndex)> GetArrayValues(string? value)
    {
        if (value is not null && value.StartsWith('\"'))
        {
            int length = value.Length > 1 && value.EndsWith('\"') ? value.Length - 1 : value.Length;
            return [..TokenListOperations.GetArrayValues(value, 0, length)];
        }
        else
        {
            return [];
        }
    }
}