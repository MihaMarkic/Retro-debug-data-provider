using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

/// <summary>
/// Token position within array.
/// </summary>
public enum PositionWithinArray
{
    /// <summary>
    /// Name.
    /// </summary>
    Name,
    /// <summary>
    /// Value.
    /// </summary>
    Value
}

/// <summary>
/// Meta data for array properties.
/// </summary>
/// <param name="Assignment"></param>
/// <param name="StartValue"></param>
/// <param name="EndValue">Exclusive</param>
/// <param name="Comma"/>
public record ArrayPropertyMeta(IToken? Assignment = null, IToken? StartValue = null, IToken? EndValue = null, IToken? Comma = null)
{
    /// <summary>
    /// An empty instance of <see cref="ArrayPropertyMeta"/>.
    /// </summary>
    public static readonly ArrayPropertyMeta Empty = new();

    internal string GetValue(ReadOnlySpan<char> content, int? maxValue = null)
    {
        if (StartValue is null || EndValue is null)
        {
            return string.Empty;
        }

        var end = maxValue ?? EndValue.StopIndex+1;
        return content[StartValue.StartIndex..end].ToString();
    }
}

internal static partial class TokenListOperations
{
    public enum TokenTypeFamily
    {
        Other,
        Directive,
    }

    internal static readonly FrozenSet<int> DirectiveTypes =
    [
        DOTTEXT, DOTENCODING, DOTFILL, DOTFILLWORD, DOTLOHIFILL, DOTCPU, DOTBYTE, DOTWORD, DOTDWORD, PRINT, PRINTNOW,
        VAR, DOTIMPORT, CONST, IF, ELSE, ERRORIF, EVAL, FOR, WHILE, STRUCT, DEFINE, FUNCTION, RETURN, MACRO,
        PSEUDOCOMMAND, PSEUDOPC, NAMESPACE, SEGMENT, SEGMENTDEF, SEGMENTOUT, MODIFY, FILEMODIFY, PLUGIN, LABEL, FILE,
        DISK, PC, BREAK, WATCH, ZP
    ];

    internal static readonly FrozenSet<int> TextTypes =
    [
        UNQUOTED_STRING, C64, BINARY, TEXT, SOURCE,
    ];

    internal static readonly FrozenSet<int> PropertyValueTypes =
    [
        STRING, BIN_NUMBER, HEX_NUMBER, DEC_NUMBER, UNQUOTED_STRING, TRUE, FALSE,
    ];

    internal static readonly FrozenSet<int> PreprocessorDirectiveTypes =
        [HASHIF, HASHELIF, HASHELSE, HASHENDIF, HASHDEFINE, HASHUNDEF, HASHIMPORT, HASHIMPORTIF, HASHIMPORTONCE];
    
    internal static bool IsTextType(this IToken token) => TextTypes.Contains(token.Type);
    internal static bool IsDirectiveType(this IToken token) => DirectiveTypes.Contains(token.Type);
    internal static bool IsPreprocessorDirectiveType(this IToken token) => PreprocessorDirectiveTypes.Contains(token.Type);
    internal static bool IsPropertyValueType(this IToken token) => PropertyValueTypes.Contains(token.Type);

    internal static TokenTypeFamily GetTokenTypeFamily(IToken token)
    {
        if (DirectiveTypes.Contains(token.Type))
        {
            return TokenTypeFamily.Directive;
        }

        return TokenTypeFamily.Other;
    }

    /// <summary>
    /// Figures out position within an array and returns data for completion.
    /// </summary>
    /// <param name="properties">Array properties usually obtained with <see cref="GetArrayProperties"/></param>
    /// <param name="content">File text content</param>
    /// <param name="absolutePosition">Absolute cursor position -1 based</param>
    /// <returns>Returns property name, type of position, text left of the position and entire value.</returns>
    internal static (IToken? Name, PositionWithinArray Position, string Root, string Value, ArrayPropertyMeta? MatchingProperty) 
        GetColumnPositionData(this FrozenDictionary<IToken, ArrayPropertyMeta> properties, ReadOnlySpan<char> content, int absolutePosition)
    {
        // when no properties, it has to be on an empty one
        if (properties.Count == 0)
        {
            return (null, PositionWithinArray.Name, "", "", null);
        }

        foreach (var pair in properties.OrderBy(p => p.Key.StartIndex))
        {
            var name = pair.Key;
            var meta = pair.Value;
            var comma = meta.Comma;
            bool isWithinProperty;
            if (comma is not null)
            {
                isWithinProperty = name.StartIndex < absolutePosition && comma.StopIndex >= absolutePosition;
            }
            else
            {
                isWithinProperty = true;
            }

            if (isWithinProperty)
            {
                var assignment = meta.Assignment;
                string value = meta.GetValue(content);
                if (assignment is null)
                {
                    return (pair.Key, PositionWithinArray.Name, content[name.StartIndex..absolutePosition].ToString(), value, meta);
                }
                else
                {
                    if (absolutePosition < assignment.StartIndex)
                    {
                        return (name, PositionWithinArray.Name, content[name.StartIndex..absolutePosition].ToString(), value, meta);
                    }
                    else
                    {
                        return (name, PositionWithinArray.Value, meta.GetValue(content, absolutePosition), value, meta);
                    }
                }
            }
        }
        return (null, PositionWithinArray.Name, "", "", null);
    }
    
    /// <summary>
    /// Returns information about array properties.
    /// </summary>
    /// <param name="tokens">Array tokens excluding open bracket</param>
    /// <returns></returns>
    internal static FrozenDictionary<IToken, ArrayPropertyMeta> GetArrayProperties(this ReadOnlySpan<IToken> tokens)
    {
        if (tokens.IsEmpty)
        {
            return FrozenDictionary<IToken, ArrayPropertyMeta>.Empty;
        }

        var result = new Dictionary<IToken, ArrayPropertyMeta>();
        var state = GetArrayPropertiesState.Comma;
        IToken? nameToken = null;
        IToken? valueStartToken = null;
        IToken? assignmentToken = null;
        int index = 0;
        while (index < tokens.Length && tokens[index].Type is not (CLOSE_BRACKET or EOL or KickAssemblerLexer.Eof) && state is not GetArrayPropertiesState.OpenString)
        {
            var token = tokens[index];
            switch (state)
            {
                case GetArrayPropertiesState.Comma:
                    if (token.IsTextType())
                    {
                        nameToken = token;
                        state = GetArrayPropertiesState.Name;
                        break;
                    }

                    break;
                case GetArrayPropertiesState.Name:
                    switch (token.Type)
                    {
                        case ASSIGNMENT:
                            state = GetArrayPropertiesState.Assignment;
                            assignmentToken = token;
                            break;
                        case COMMA:
                            result.Add(nameToken!, new ArrayPropertyMeta(Comma: token));
                            nameToken = assignmentToken = null;
                            state = GetArrayPropertiesState.Comma;
                            break;
                        default:
                            nameToken = null;
                            state = GetArrayPropertiesState.Comma;
                            break;
                    }

                    break;
                case GetArrayPropertiesState.Assignment:
                    switch (token.Type)
                    {
                        case COMMA:
                            result.Add(nameToken!, new ArrayPropertyMeta(assignmentToken!, Comma: token));
                            nameToken = assignmentToken = null;
                            state = GetArrayPropertiesState.Comma;
                            nameToken = null;
                            break;
                        case OPEN_STRING:
                            valueStartToken = token;
                            state = GetArrayPropertiesState.OpenString;
                            break;
                        default:
                            valueStartToken = token;
                            state = GetArrayPropertiesState.Value;
                            break;
                    }

                    break;
                case GetArrayPropertiesState.Value:
                    switch (token.Type)
                    {
                        case COMMA:
                            result.Add(nameToken!, new ArrayPropertyMeta(assignmentToken!, valueStartToken!, tokens[index-1], Comma: token));
                            nameToken = assignmentToken = valueStartToken = null;
                            state = GetArrayPropertiesState.Comma;
                            break;
                    }

                    break;
            }

            index++;
        }

        switch (state)
        {
            case GetArrayPropertiesState.Name:
                result.Add(nameToken!, ArrayPropertyMeta.Empty);
                break;
            case GetArrayPropertiesState.Assignment:
                result.Add(nameToken!, new ArrayPropertyMeta(assignmentToken));
                break;
            case GetArrayPropertiesState.Value:
                result.Add(nameToken!, new ArrayPropertyMeta(assignmentToken!, valueStartToken!, tokens[index-1]));
                break;
            case GetArrayPropertiesState.OpenString:
                // in case of open string take everything up to end of line (or EOF)
                int endOfLine = index;
                while (tokens[endOfLine].Type is not (EOL or KickAssemblerLexer.Eof))
                {
                    endOfLine++;
                }
                result.Add(nameToken!, new ArrayPropertyMeta(assignmentToken!, valueStartToken!, tokens[endOfLine-1]));
                break;
        }

        return result.ToFrozenDictionary();
    }

    private enum GetArrayPropertiesState
    {
        Name,
        Assignment,
        Value,
        OpenString,
        Comma,
    }
    
    [GeneratedRegex("""
                    ^"(\s*(?<ArrayItem>[^,"]*)\s*,)*\s*(?<LastItem>[^,"]*)?
                    """, RegexOptions.Singleline)]
    private static partial Regex GetArrayValuesRegex();
    /// <summary>
    /// Collects all coma delimited values within a string.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    internal static ImmutableArray<(string Text, int StartIndex)> GetArrayValues(string text, int start, int length)
    {
        var m = GetArrayValuesRegex().Match(text, start, length);
        if (m.Success)
        {
            var items = m.Groups["ArrayItem"].Captures
                .Where(c => !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => (c.Value, c.Index))
                .ToImmutableArray();
            var lastItemGroup = m.Groups["LastItem"]; 
            string? lastItem = lastItemGroup.Value;
            if (!string.IsNullOrWhiteSpace(lastItem))
            {
                return items.Add((lastItem, lastItemGroup.Index));
            }

            return items;
        }

        return [];
    }

    /// <summary>
    /// Finds directive and optional keyword.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    internal static (IToken DirectiveToken, IToken? OptionToken)? FindDirectiveAndOption(ReadOnlySpan<IToken> tokens)
    {
        if (tokens.IsEmpty)
        {
            return null;
        }

        var token = tokens[^1];
        if (GetTokenTypeFamily(token) == TokenTypeFamily.Directive)
        {
            return (token, null);
        }

        if (tokens.Length > 1 && token.IsTextType())
        {
            var previousToken = tokens[^2];
            if (GetTokenTypeFamily(previousToken) == TokenTypeFamily.Directive)
            {
                return (previousToken, token);
            }
        }

        return null;
    }

    /// <summary>
    /// Skips an array starting with close bracket.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="isMandatory">When true, array has to exist, otherwise it is optional</param>
    /// <returns>Token index left of open bracket if array is skipped or optional, null otherwise.</returns>
    internal static int? SkipArray(ReadOnlySpan<IToken> tokens, bool isMandatory)
    {
        if (tokens.IsEmpty)
        {
            return null;
        }

        var index = GetNextNonEolToken(tokens, tokens.Length - 1);
        if (index is null)
        {
            return null;
        }
        var token = tokens[index.Value];
        if (token.Type != CLOSE_BRACKET)
        {
            return isMandatory ? null : tokens.Length - 1;
        }

        var result = FindWithinArrayOpenBracket(tokens[..index.Value]);
        if (result is not null)
        {
            return result.Value - 1;
        }

        return null;
    }

    static int? GetNextNonEolToken(ReadOnlySpan<IToken> tokens, int currentIndex)
    {
        while (currentIndex >= 0 && tokens[currentIndex].Type == EOL)
        {
            currentIndex--;
        }

        return currentIndex >= 0 ? currentIndex: null;
    }
    /// <summary>
    /// Find open brace that contains arrays.
    /// Must start outside an array.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal static int? FindBodyStartForArrays(ReadOnlySpan<IToken> tokens)
    {
        if (tokens.Length == 0)
        {
            return null;
        }

        int? index = GetNextNonEolToken( tokens, tokens.Length - 1);
        if (index is null)
        {
            return null;
        }
        var token = tokens[index.Value];
        var state = token.Type switch
        {
            COMMA => FindBodyStartForArraysState.Comma,
            OPEN_BRACE => FindBodyStartForArraysState.OpenBrace,
            CLOSE_BRACKET => FindBodyStartForArraysState.Array,
            _ => FindBodyStartForArraysState.Invalid,
        };
        while (state is not (FindBodyStartForArraysState.OpenBrace or FindBodyStartForArraysState.Invalid))
        {
            index = GetNextNonEolToken(tokens, index!.Value - 1);
            if (index is null)
            {
                state = FindBodyStartForArraysState.Invalid;
                break;
            }
            token = tokens[index.Value];
            switch (state)
            {
                case FindBodyStartForArraysState.Comma:
                    state = token.Type switch
                    {
                        CLOSE_BRACKET => FindBodyStartForArraysState.Array,
                        _ => FindBodyStartForArraysState.Invalid,
                    };
                    break;
                case FindBodyStartForArraysState.Array:
                    int? openBracketIndex = FindWithinArrayOpenBracket(tokens[..(index.Value + 1)]);
                    if (openBracketIndex is not null)
                    {
                        index = openBracketIndex.Value;
                        state = FindBodyStartForArraysState.OpenBracket;
                    }
                    else
                    {
                        state = FindBodyStartForArraysState.Invalid;
                    }

                    break;
                case FindBodyStartForArraysState.OpenBracket:
                    state = token.Type switch
                    {
                        COMMA => FindBodyStartForArraysState.Comma,
                        OPEN_BRACE => FindBodyStartForArraysState.OpenBrace,
                        _ => FindBodyStartForArraysState.Invalid,
                    };
                    break;
            }
        }

        return state switch
        {
            FindBodyStartForArraysState.OpenBrace => index!.Value,
            _ => null,
        };
    }

    private enum FindBodyStartForArraysState
    {
        Invalid,
        Array,
        Comma,
        OpenBrace,
        OpenBracket,
    }

    /// <summary>
    /// Looks for given token type in the current line. Starts from end.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="type"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    internal static bool GetLastIndexOf(this ReadOnlySpan<IToken> tokens, int type, [NotNullWhen(true)]out int? index)
    {
        for (int i = tokens.Length - 1; i >= 0; i--)
        {
            if (tokens[i].Type == type)
            {
                index = i;
                return true;
            }
            else if (tokens[i].Type == EOL)
            {
                break;
            }
        }

        index = null;
        return false;
    }
    
    // internal static int? 
    /// <summary>
    /// Finds array open bracket from the end of the tokens towards start.
    /// If array is malformed, null is returned.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    internal static int? FindWithinArrayOpenBracket(this ReadOnlySpan<IToken> tokens)
    {
        if (tokens.IsEmpty)
        {
            return null;
        }

        // there can be only a single DOUBLE_QUOTE type within the line, others would be STRINGs
        if (tokens.GetLastIndexOf(OPEN_STRING, out var lastDoubleQuoteIndex))
        {
            if (lastDoubleQuoteIndex == 0)
            {
                return null;
            }

            tokens = tokens[..lastDoubleQuoteIndex.Value];
        }

        if (tokens.Length == 1)
        {
            if (tokens[0].Type == OPEN_BRACKET)
            {
                return 0;
            }
    
            return null;
        }
        
        var previousToken = tokens[^2];
        FindArrayOpenBracketState state;
        var current = tokens[^1];
        if (current.IsTextType())
        {
            state = previousToken.Type switch
            {
                ASSIGNMENT => FindArrayOpenBracketState.StringValue,
                COMMA or OPEN_BRACKET => FindArrayOpenBracketState.PropertyName,
                // TODO eventually handle properly " since going backward is unclear whether it's a string or not, i.e. [seg="oo
                OPEN_STRING => FindArrayOpenBracketState.OpenStringValue,
                _ => FindArrayOpenBracketState.Invalid,
            };
        }
        else
        {
            state = current.Type switch
            {
                ASSIGNMENT => FindArrayOpenBracketState.Assignment,
                COMMA => FindArrayOpenBracketState.Comma,
                OPEN_BRACKET => FindArrayOpenBracketState.OpenBracket,
                OPEN_STRING => FindArrayOpenBracketState.Value,
                _ => IsPropertyValueType(current) ? FindArrayOpenBracketState.Value : FindArrayOpenBracketState.Invalid,
            };
        }

        int index = tokens.Length - 1;
        if (tokens.Length > 1)
        {
            while (state is not (FindArrayOpenBracketState.OpenBracket or FindArrayOpenBracketState.Invalid))
            {
                index--;
                if (index < 0)
                {
                    state = FindArrayOpenBracketState.Invalid;
                    break;
                }
                var token = tokens[index];
                switch (state)
                {
                    case FindArrayOpenBracketState.Value:
                        switch (token.Type)
                        {
                            case ASSIGNMENT:
                                state = FindArrayOpenBracketState.Assignment;
                                break;
                            default:
                                if (!IsPropertyValueType(token))
                                {
                                    state = FindArrayOpenBracketState.Invalid;
                                }
    
                                break;
                        }
                        break;
                    case FindArrayOpenBracketState.OpenStringValue:
                        state = token.Type switch
                        {
                            OPEN_STRING => FindArrayOpenBracketState.Value,
                            _ => state,
                        };
                        break;
                    case FindArrayOpenBracketState.StringValue:
                        state = token.Type switch
                        {
                            COMMA => FindArrayOpenBracketState.Comma,
                            ASSIGNMENT => FindArrayOpenBracketState.Assignment,
                            OPEN_BRACKET => FindArrayOpenBracketState.OpenBracket,
                            _ => FindArrayOpenBracketState.Invalid,
                        };
    
                        break;
                    case FindArrayOpenBracketState.Assignment:
                        if (token.IsTextType())
                        {
                            state = FindArrayOpenBracketState.PropertyName;
                        }
                        else
                        {
                            state = FindArrayOpenBracketState.Invalid;
                        }
    
                        break;
                    case FindArrayOpenBracketState.PropertyName:
                        state = token.Type switch
                        {
                            OPEN_BRACKET => FindArrayOpenBracketState.OpenBracket,
                            COMMA => FindArrayOpenBracketState.Comma,
                            _ => FindArrayOpenBracketState.Invalid
                        };
                        break;
                    case FindArrayOpenBracketState.Comma:
                        if (token.IsTextType())
                        {
                            state = FindArrayOpenBracketState.StringValue;
                        }
                        else
                        {
                            state = token.Type switch
                            {
                                STRING or BIN_NUMBER or HEX_NUMBER or DEC_NUMBER =>
                                    FindArrayOpenBracketState.Value,
                                _ => FindArrayOpenBracketState.Invalid
                            };
                        }
    
                        break;
                }
            }
        }
    
        switch (state)
        {
            case FindArrayOpenBracketState.OpenBracket:
                return index;
            case FindArrayOpenBracketState.Invalid:
            default:
                return null;
        }
    }
    
    private enum FindArrayOpenBracketState
    {
        Invalid,
        Value,
    
        // text without double or any quotes
        StringValue,
        // value starting with " but not ending
        OpenStringValue,
        Assignment,
        Comma,
        PropertyName,
        OpenBracket,
    }

    /// <summary>
    /// Returns token index containing <paramref name="column"/>.
    /// When cursor is right in front of token, such as |4, it returns token 4 but only when there is space/tab to the left. 
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="lineStart"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    internal static int? GetTokenIndexAtColumn(this ReadOnlySpan<IToken> tokens, int lineStart, int column)
    {
        int position = lineStart + column;
        for (int i = 0; i < tokens.Length; i++)
        {
            var t = tokens[i];
            if (t.StartIndex > position)
            {
                break;
            }

            if (t.StopIndex >= position - 1)
            {
                // check situation 
                if (i > 0 && t.StartIndex == position)
                {
                    var previous = tokens[i - 1];
                    if (previous.StopIndex < t.StartIndex - 1)
                    {
                        return i;
                    }
                }
                else
                {
                    return i;
                }
            }
        }

        return null;
    }

    internal static int? GetTokenIndexToTheLeftOfColumn(this ReadOnlySpan<IToken> tokens, int lineStart, int column)
    {
        int position = lineStart + column;
        for (int i = tokens.Length-1; i >= 0; i--)
        {
            var t = tokens[i];
            if (t.StopIndex < position)
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns token to the left of last token but only if it is attached to it (no space in between).
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    internal static int? GetAttachedTokenToTheLeft(ReadOnlySpan<IToken> tokens)
    {
        if (tokens.Length < 2)
        {
            return null;
        }
        var current = tokens[^1];
        var previous = tokens[^2];
        if (current.StartIndex == previous.StopIndex + 1)
        {
            return tokens.Length - 2;
        }

        return null;
    }
}