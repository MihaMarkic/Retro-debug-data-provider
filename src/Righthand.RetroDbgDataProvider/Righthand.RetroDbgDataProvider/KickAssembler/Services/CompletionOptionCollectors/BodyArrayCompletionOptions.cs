using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;

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
    /// <returns></returns>
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> tokens, string content, int lineStart, int lineLength,
        int column)
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
        var bodyStartIndex = TokenListOperations.FindBodyStartForArrays(tokens[..(openBracketIndex.Value)]);
        if (bodyStartIndex is null)
        {
            Debug.WriteLine("Couldn't find body start");
            return null;
        }

        var statementBeforeArrayStartIndex = TokenListOperations.SkipArray(tokens[..bodyStartIndex.Value], isMandatory: false);
        if (statementBeforeArrayStartIndex is null || statementBeforeArrayStartIndex.Value < 0)
        {
            Debug.WriteLine("Couldn't find statement array start");
            return null;
        }

        var statement = TokenListOperations.FindDirectiveAndOption(tokens[..(statementBeforeArrayStartIndex.Value + 1)]);
        if (statement is null)
        {
            Debug.WriteLine("Couldn't find statement directive and option");
            return null;
        }

        if (statement.Value.Directive != KickAssemblerLexer.DISK)
        {
            Debug.WriteLine("Body arrays currently only supported with .disk directive");
            return null;
        }

        var arrayProperties = TokenListOperations.GetArrayProperties(tokens[(openBracketIndex.Value + 1)..]);
        int absoluteColumn = lineStart + column + 1;
        var (name, position, root, value) = TokenListOperations.GetColumnPositionData(arrayProperties, content, absoluteColumn);

        int replacementLength;
        switch (position)
        {
            case PositionWithinArray.Name:
                replacementLength = name is not null ? name.StopIndex - absoluteColumn + 1 : 0;
                var excludedValues = arrayProperties
                    .Where(p => p.Key != name)
                    .Select(p => p.Key.Text)
                    .Distinct().ToFrozenSet();
                return new CompletionOption(CompletionOptionType.ArrayPropertyName, root, false, replacementLength, excludedValues,
                    ".DISK_FILE");
            case PositionWithinArray.Value:
                replacementLength = value?.Length ?? 0;
                bool endsWithDoubleQuote;
                if (root.StartsWith('"'))
                {
                    root = root.Substring(1);
                    replacementLength--;
                    endsWithDoubleQuote = root.EndsWith('"');
                }
                else
                {
                    endsWithDoubleQuote = false;
                }
                return new CompletionOption(CompletionOptionType.ArrayPropertyValue, root, endsWithDoubleQuote, replacementLength, FrozenSet<string>.Empty,
                    ".DISK_FILE", name!.Text);
            default:
                throw new Exception($"Invalid position {position}");
        }

    }
}