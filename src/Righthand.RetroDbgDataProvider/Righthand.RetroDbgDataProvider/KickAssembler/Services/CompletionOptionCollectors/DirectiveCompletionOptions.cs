using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

/// <summary>
/// Evaluates completion options for directives.
/// </summary>
public static class DirectiveCompletionOptions
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="lineTokens"></param>
    /// <param name="text"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineLength"></param>
    /// <param name="column"></param>
    /// <param name="relativePath">Relative path to either project or library, depends on the file origin</param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int column, string relativePath, CompletionOptionContext context)
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
                                suggestions = CompletionOptionCollectorsCommon.CollectFileSystemSuggestions(relativePath, root, fileExtensions, excluded, context.ProjectServices);
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
                        case ".const":
                        case ".var":
                        case ".enum":
                        case ".break":
                        case ".macro":
                        case ".namespace":
                        case ".segment":
                        case ".while":
                            // disables completion where it doesn't make sense
                            return CompletionOption.Empty;
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

    /// <summary>
    /// Token position type.
    /// </summary>
    public enum PositionType
    {
        /// <summary>
        /// At directive.
        /// </summary>
        Directive,
        /// <summary>
        /// At directive type.
        /// </summary>
        Type,
        /// <summary>
        /// At directive value.
        /// </summary>
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

        // first check whether current token is .
        if (currentToken.Type == DOT_UNQUOTED_STRING)
        {
            return new(PositionType.Directive, "", null, currentToken.Text, "", currentToken.Length(), false);
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
        IToken? paramsToken = null;
        PositionType? positionType = null;
        var root = "";
        string value = "";
        int replacementLength = 0;
        bool hasEndDelimiter = false;
        if (index < lineTokens.Length)
        {
            var nextToken = lineTokens[index];
            if (nextToken.IsTextType())
            {
                paramsToken = nextToken;
                index++;
            }

            if (index < lineTokens.Length)
            {
                nextToken = lineTokens[index];
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
            }
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
                if (positionType == PositionType.Directive)
                {
                    root = directiveToken.Text;
                    replacementLength = root.Length;
                }
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