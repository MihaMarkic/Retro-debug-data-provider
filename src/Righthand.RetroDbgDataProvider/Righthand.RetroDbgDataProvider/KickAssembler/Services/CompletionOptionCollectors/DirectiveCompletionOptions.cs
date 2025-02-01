using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class DirectiveCompletionOptions
{
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int column, CompletionOptionContext context)
    {
        Debug.WriteLine($"Trying {nameof(DirectiveCompletionOptions)}");
        var line = text.AsSpan()[lineStart..(lineStart + lineLength)];
        if (line.Length == 0)
        {
            return null;
        }
        var lineMeta = GetMetaInformation(lineTokens, text, lineStart, lineLength, column);
        if (lineMeta is not null)
        {
            var (position, keyword, parameter, root, currentValue, replacementLength, hasEndDelimiter) = lineMeta.Value;
            FrozenSet<Suggestion> suggestions;
            switch (position)
            {
                case PositionType.Directive:
                    suggestions = [..DirectiveProperties.GetDirectives(root).Select(v => new StandardSuggestion(SuggestionOrigin.DirectiveOption, v))];
                    return new CompletionOption(root, replacementLength, string.Empty, "", suggestions);
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
                                suggestions = CompletionOptionCollectorsCommon.CollectFileSystemSuggestions(root, fileExtensions, excluded, context.ProjectServices);
                                return new CompletionOption(root, replacementLength, string.Empty, hasEndDelimiter ? "" : "\"", suggestions);
                            }
                        }
                        else
                        {
                            var enumerationValues = directiveValues.Items.OfType<EnumerableDirectiveValueType>().ToFrozenSet();
                            if (enumerationValues.Any())
                            {
                                suggestions = enumerationValues
                                    .Where(v => v.Value.StartsWith(root))
                                    .Select(v => new StandardSuggestion(SuggestionOrigin.DirectiveOption, v.Value))
                                    .Cast<Suggestion>()
                                    .ToFrozenSet();
                                return new CompletionOption(root, replacementLength, string.Empty, hasEndDelimiter ? "" : "\"", suggestions);
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

                                suggestions =
                                    directiveWithType.ValueTypes.Keys
                                        .Where(k => k.StartsWith(root))
                                        .Select(k => new StandardSuggestion(SuggestionOrigin.DirectiveOption, k, 0) { IsDefault = k == defaultSuggestion })
                                        .Cast<Suggestion>()
                                        .ToFrozenSet();
                                return new CompletionOption(root, replacementLength, string.Empty, string.Empty, suggestions);
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
        Directive,
        Type,
        Value,
    }

    internal static LineMeta? GetMetaInformation(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int lineCursor)
    {
        int absoluteLineCursor = lineStart + lineCursor;
        var cursorTokenIndex = lineTokens.GetTokenIndexAtColumn(0, absoluteLineCursor);
        cursorTokenIndex ??= lineTokens.GetTokenIndexToTheLeftOfColumn(0, absoluteLineCursor);
        if (cursorTokenIndex is null)
        {
            return null;
        }

        int index = cursorTokenIndex.Value;
        var currentToken = lineTokens[index];
        var previousToken = index > 0 ? lineTokens[index - 1] : null;
        IToken? nextToken;
        string root;

        // first check whether current token is .
        if (currentToken.Type == DOT)
        {
            nextToken = index + 1 < lineTokens.Length ? lineTokens[index + 1] : null;
            if (nextToken is not null && nextToken.StartIndex == currentToken.StopIndex && nextToken.IsTextType())
            {
                return new(PositionType.Directive, "", null, ".", "", nextToken.Length() + 1, false);
            }
            return new(PositionType.Directive, "", null, ".", "", 1, false);
        }

        // then check whether previous is . and current is text like .something
        if (previousToken is not null && previousToken.Type == DOT && previousToken.StopIndex == currentToken.StartIndex - 1)
        {
            string rootDirectiveName = currentToken.TextUpToColumn(lineStart + lineCursor);
            root = $".{rootDirectiveName}";
            return new(PositionType.Directive, "", null, root, "", root.Length, false);
        }

        int startOfStringIndex = -1;
        if (currentToken.Type is OPEN_STRING or STRING)
        {
            startOfStringIndex = index;
        }
        else
        {
            var upToCursorTokens = lineTokens[..index];
            if (upToCursorTokens.GetLastIndexOf(OPEN_STRING, out var temp))
            {
                startOfStringIndex = temp.Value;
            }
        }

        ReadOnlySpan<IToken> upToDoubleQuote = startOfStringIndex < 0 ? lineTokens[..(index+1)] : lineTokens[..startOfStringIndex];
        index = upToDoubleQuote.Length - 1;
        while (index >= 0 && !upToDoubleQuote[index].IsDirectiveType())
        {
            index--;
        }

        if (index < 0)
        {
            return null;
        }

        var directiveToken = lineTokens[index];
        index++;
        nextToken = lineTokens[index];
        IToken? paramsToken = null;
        PositionType? positionType = null;
        root = "";
        if (nextToken.IsTextType())
        {
            paramsToken = nextToken;
            index++;
        }

        string value = "";
        nextToken = lineTokens[index];
        int replacementLength = 0;
        bool hasEndDelimiter = false;
        switch (nextToken.Type)
        {
            case STRING:
                value = nextToken.Text.Trim('\"');
                if (nextToken.ContainsColumn(absoluteLineCursor))
                {
                    positionType = PositionType.Value;
                    root = nextToken.TextUpToColumn(absoluteLineCursor).Trim('\"');
                    replacementLength = value.Length;
                    hasEndDelimiter = true;
                }

                break;
            case OPEN_STRING:
                value = nextToken.Text.TrimStart('\"');
                if (nextToken.ContainsColumn(absoluteLineCursor))
                {
                    positionType = PositionType.Value;
                    root = nextToken.TextUpToColumn(absoluteLineCursor).TrimStart('\"');
                    replacementLength = value.Length;
                    hasEndDelimiter = false;
                }

                break;
        }

        if (positionType is null)
        {
            if (paramsToken is not null)
            {
                if (paramsToken.ContainsColumnWithInclusiveEdge(absoluteLineCursor))
                {
                    root = paramsToken.ContainsColumn(absoluteLineCursor) ? paramsToken.TextUpToColumn(absoluteLineCursor): "";
                    replacementLength = paramsToken.Text.Length;
                    positionType = PositionType.Type;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                positionType = directiveToken.ContainsColumnWithInclusiveEdge(absoluteLineCursor) ? PositionType.Directive: PositionType.Type;
            }
        }

        return new(positionType!.Value, directiveToken.Text, paramsToken?.Text, root, value, replacementLength, hasEndDelimiter);

    }
    
    /// <summary>
    /// Holds information about line.
    /// </summary>
    /// <param name="PositionType"></param>
    /// <param name="Directive"></param>
    /// <param name="Parameter"></param>
    /// <param name="Root"></param>
    /// <param name="CurrentValue"></param>
    /// <param name="ReplacementLength"></param>
    /// <param name="HasEndDelimiter"></param>
    internal record struct LineMeta(
        PositionType PositionType,
        string Directive,
        string? Parameter,
        string Root,
        string CurrentValue,
        int ReplacementLength,
        bool HasEndDelimiter);
}