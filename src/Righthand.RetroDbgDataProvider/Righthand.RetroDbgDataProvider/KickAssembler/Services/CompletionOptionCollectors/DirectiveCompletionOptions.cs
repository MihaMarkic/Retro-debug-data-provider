using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

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
        
        var cursorTokenIndex = TokenListOperations.GetTokenIndexAtColumn(lineTokens, 0, column);
        if (cursorTokenIndex is not null)
        {
            var currentToken = lineTokens[cursorTokenIndex.Value];
            if (currentToken.Text.StartsWith('.'))
            {
                
            }
        }

        var lineMeta = GetMetaInformation(lineTokens, text, lineStart, lineLength, column);
        if (lineMeta is not null)
        {
            var (position, keyword, parameter, root, currentValue, replacementLength, hasEndDelimiter) = lineMeta.Value;
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
        Directive,
        Type,
        Value,
    }

    internal static LineMeta? GetMetaInformation(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int lineCursor)
    {
        var cursorTokenIndex = TokenListOperations.GetTokenIndexAtColumn(lineTokens, 0, lineCursor);
        if (cursorTokenIndex is not null)
        {
            int index = cursorTokenIndex.Value;
            var currentToken = lineTokens[index];
            var previousToken = index > 0 ? lineTokens[index - 1] : null;
            if (currentToken.Type != DOT || previousToken?.Type == DOT && currentToken.IsTextType())
            {
                int startOfStringIndex = -1;
                if (currentToken.Type is DOUBLE_QUOTE or STRING)
                {
                    startOfStringIndex = index;
                }
                else
                {
                    var upToCursorTokens = lineTokens[..index];
                    if (upToCursorTokens.GetLastIndexOf(DOUBLE_QUOTE, out var temp))
                    {
                        startOfStringIndex = temp.Value;
                    }
                }

                ReadOnlySpan<IToken> upToDoubleQuote = startOfStringIndex < 0 ? lineTokens[..index] : lineTokens[..startOfStringIndex];
                index = upToDoubleQuote.Length - 1;
                while (index >= 0 && !upToDoubleQuote[index].IsDirectiveType())
                {
                    index--;
                }

                if (index < 0)
                {
                    return null;
                }
            }
            
            var directiveToken = lineTokens[index];
            int absoluteLineCursor = lineStart + lineCursor;
            if (!directiveToken.IsDirectiveType())
            {
                var length = absoluteLineCursor - directiveToken.StartIndex;
                var root = directiveToken.Text[..length];
                return new(PositionType.Directive, directiveToken.Text, null, root, "", directiveToken.Text.Length, false);
            }
            else
            {
                index++;
                var nextToken = lineTokens[index];
                IToken? paramsToken = null;
                PositionType? positionType = null;
                var root = "";
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
                            root = nextToken.TextUpToColumn(absoluteLineCursor);
                            replacementLength = value.Length;
                            hasEndDelimiter = true;
                        }

                        break;
                    case DOUBLE_QUOTE:
                        var lastToken = lineTokens[^1];
                        value = text[(nextToken.StartIndex + 1)..(lastToken.StopIndex)];
                        if (nextToken.StopIndex < absoluteLineCursor)
                        {
                            positionType = PositionType.Value;
                            root = text[(nextToken.StartIndex + 1)..absoluteLineCursor];
                            replacementLength = value.Length;
                        }

                        break;
                }

                if (positionType is null)
                {
                    positionType = PositionType.Type;
                    if (paramsToken?.ContainsColumn(absoluteLineCursor) ?? false)
                    {
                        root = paramsToken.TextUpToColumn(absoluteLineCursor);
                        replacementLength = paramsToken.Text.Length;
                    }
                }

                return new(positionType!.Value, directiveToken.Text, paramsToken?.Text, root, value, replacementLength, hasEndDelimiter);
            }
        }

        return null;
    }
    [GeneratedRegex("""
                    (?<FullKeyWord>\.(?<KeyWord>([a-zA-Z]+)))(?<ParameterSpace>\s+(?<Parameter>\w*)\s*)?(?<Value>(?<StartDoubleQuote>")(?<CurrentValue>[^"]+)?(?<EndDoubleQuote>")?)?
                    """, RegexOptions.Singleline)]
    private static partial Regex QuotedValueTemplateRegex();

    internal static LineMeta? GetMetaInformationX(string text, int lineStart, int lineLength,
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