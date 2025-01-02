using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Services.Abstract;

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
    /// <param name="column"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> tokens, string content, int lineStart, int lineLength,
        int column, CompletionOptionContext context)
    {
        var columnTokenIndex = TokenListOperations.GetTokenIndexAtColumn(tokens, lineStart, column);
        if (columnTokenIndex is null)
        {
            return null;
        }

        var columnToken = tokens[columnTokenIndex.Value];
        int? openBracketIndex = columnToken.Type == KickAssemblerLexer.OPEN_BRACKET 
            ? columnTokenIndex.Value: TokenListOperations.FindWithinArrayOpenBracket(tokens[..columnTokenIndex.Value]);
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

        var arrayProperties = TokenListOperations.GetArrayProperties(tokens[(openBracketIndex.Value + 1)..]);
        int absoluteColumn = lineStart + column + 1;
        var (name, position, root, value, matchingArrayProperty) = TokenListOperations.GetColumnPositionData(arrayProperties, content, absoluteColumn);

        switch (position)
        {
            case PositionWithinArray.Name:
                var replacementLength = name is not null ? name.StopIndex - absoluteColumn + 1 : 0;
                var suggestedValues = ArrayProperties
                    .GetNames(arrayOwner, root)
                    .Select(v => new StandardSuggestion(SuggestionOrigin.PropertyName, v))
                    .Cast<Suggestion>()
                    .ToFrozenSet();
                return new CompletionOption(root, replacementLength, "", suggestedValues);
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
        int replacementLength = value?.Length ?? 0;
        bool endsWithDoubleQuote;
        // if (root.StartsWith('"'))
        // {
        //     root = root.Substring(1);
        //     replacementLength--;
        //     endsWithDoubleQuote = root.EndsWith('"');
        // }
        // else
        {
            endsWithDoubleQuote = false;
        }

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
                    var values = GetArrayValues(value);
                    bool startsWithDoubleQuote = value.StartsWith('\"');
                    string valueRoot = GetRootValue(values, absoluteColumn - matchingProperty.StartValue.StartIndex + (startsWithDoubleQuote ? 1 : 0));
                    var valueTexts = values.Select(v => v.Text).ToImmutableArray();
                    if (!string.IsNullOrWhiteSpace(option))
                    {
                        valueTexts = valueTexts.Add(option);
                    }
                    var excluded = valueTexts.Distinct().ToFrozenSet();
                    suggestionTexts = CompletionOptionCollectorsCommon.CollectSegmentsSuggestions(valueRoot, excluded, context.ProjectServices);
                    suggestions = CompletionOptionCollectorsCommon.CreateSuggestionsFromTexts(valueRoot, suggestionTexts, SuggestionOrigin.PropertyValue);
                }

                break;
            }
            case ArrayPropertyType.FileNames:
            {
                var excluded = GetArrayValues(value).Select(v => v.Text).Distinct().ToFrozenSet();
                var property = (FileArrayProperty)arrayProperty;
                suggestions = CompletionOptionCollectorsCommon.CollectFileSuggestions(root, property.ValidExtensions, excluded, context.ProjectServices);
                break;
            }
        }
        return new CompletionOption(root, replacementLength, endsWithDoubleQuote ? "\"" : "", suggestions);
    }

    internal static string GetRootValue(ImmutableArray<(string Text, int StartIndex)> values, int relativeColumn)
    {
        foreach (var v in values)
        {
            if (v.StartIndex <= relativeColumn && v.StartIndex + v.Text.Length >= relativeColumn)
            {
                return v.Text[..(relativeColumn - v.StartIndex)];
            }
        }

        return string.Empty;
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