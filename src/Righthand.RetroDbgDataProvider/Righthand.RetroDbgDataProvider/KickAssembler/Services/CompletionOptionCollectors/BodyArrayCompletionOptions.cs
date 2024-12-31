using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class BodyArrayCompletionOptions
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
                    .Select(v => new Suggestion(SuggestionOrigin.PropertyName, v))
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

        FrozenSet<string> suggestionTexts = [];
        switch (arrayProperty.Type)
        {
            case ArrayPropertyType.Bool:
                suggestionTexts = [.. ArrayPropertyValues.BoolValues];
                break;
            case ArrayPropertyType.QuotedEnumerable:
            {
                if (arrayProperty is ValuesArrayProperty valuesProperty)
                {
                    suggestionTexts = valuesProperty.Values?.Select(v => $"\"{v}\"").ToFrozenSet() ?? [];
                }
            }

                break;
            case ArrayPropertyType.Enumerable:
            {
                if (arrayProperty is ValuesArrayProperty valuesProperty)
                {
                    suggestionTexts = valuesProperty.Values ?? [];
                }
            }

                break;
            case ArrayPropertyType.Segments:
                FrozenSet<string> excluded = [];
                suggestionTexts = CollectSegmentsSuggestions(root, excluded, context.SourceFiles);
                break;
        }

        FrozenSet<Suggestion> suggestions =
            suggestionTexts.Count > 0 
                ? suggestionTexts
                    .Where(t => t.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    .Select(t => new Suggestion(SuggestionOrigin.PropertyValue, t)).ToFrozenSet() 
                : [];
        return new CompletionOption(root, replacementLength, endsWithDoubleQuote ? "\"" : "", suggestions);
    }

    internal static FrozenSet<string> CollectSegmentsSuggestions(string rootText, FrozenSet<string> excluded, ISourceCodeParser<ParsedSourceFile> sourceFiles)
    {
        var builder = new List<string>();
        var allFiles = sourceFiles.AllFiles;
        foreach (var k in allFiles.Keys)
        {
            Debug.WriteLine($"Looking at {k}");
            var fileWithSet = allFiles.GetValueOrDefault(k);
            if (fileWithSet is not null)
            {
                foreach (var s in fileWithSet.AllDefineSets)
                {
                    Debug.WriteLine($"\tLooking at set {string.Join(", ", s)}");
                    var parsedSourceFile = allFiles.GetFileOrDefault(k, s);
                    if (parsedSourceFile is not null)
                    {
                        foreach (var si in parsedSourceFile.SegmentDefinitions)
                        {
                            if (!excluded.Contains(si.Name) &&
                                si.Name.StartsWith(rootText, StringComparison.Ordinal))
                            {
                                builder.Add(si.Name);
                            }
                            else
                            {
                                Debug.WriteLine($"Segment {si.Name} is excluded");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to get parsed source file");
                    }
                }
            }
        }

        return builder.Distinct().ToFrozenSet();
    }
}