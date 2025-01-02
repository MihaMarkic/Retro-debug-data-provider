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
            if (statement.Value.Directive != KickAssemblerLexer.DISK)
            {
                Debug.WriteLine("Body arrays currently only supported with .disk directive");
                return null;
            }
            arrayOwner = ".DISK_FILE";
        }
        else
        {
            var directiveToken = tokens[statement.Value.Directive];
            arrayOwner = directiveToken.Text;
        }

        var arrayProperties = TokenListOperations.GetArrayProperties(tokens[(openBracketIndex.Value + 1)..]);
        int absoluteColumn = lineStart + column + 1;
        var (name, position, root, value) = TokenListOperations.GetColumnPositionData(arrayProperties, content, absoluteColumn);

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
                    return CreateSuggestionsForArrayValue(root, name!.Text, value, propertyMeta, context);
                }

                break;
            default:
                throw new Exception($"Invalid position {position}");
        }

        return null;
    }

    internal static CompletionOption CreateSuggestionsForArrayValue(string root, string propertyName, string? value, ArrayProperty arrayProperty,
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
                var excluded = GetArrayValues(value);
                suggestionTexts = CompletionOptionCollectorsCommon.CollectSegmentsSuggestions(root, excluded, context.ProjectServices);
                suggestions = CompletionOptionCollectorsCommon.CreateSuggestionsFromTexts(root, suggestionTexts, SuggestionOrigin.PropertyValue);
                break;
            }
            case ArrayPropertyType.FileNames:
            {
                var excluded = GetArrayValues(value);
                var property = (FileArrayProperty)arrayProperty;
                suggestions = CompletionOptionCollectorsCommon.CollectFileSuggestions(root, property.ValidExtensions, excluded, context.ProjectServices);
                break;
            }
        }
        return new CompletionOption(root, replacementLength, endsWithDoubleQuote ? "\"" : "", suggestions);
    }

    internal static FrozenSet<string> GetArrayValues(string? value)
    {
        if (value is not null && value.StartsWith('\"'))
        {
            int length = value.Length > 1 && value.EndsWith('\"') ? value.Length - 1 : value.Length;
            return TokenListOperations.GetArrayValues(value, 0, length).Distinct().ToFrozenSet();
        }
        else
        {
            return [];
        }
    }
}