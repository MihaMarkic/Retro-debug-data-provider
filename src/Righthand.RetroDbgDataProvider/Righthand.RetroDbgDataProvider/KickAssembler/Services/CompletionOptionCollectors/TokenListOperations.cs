using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Antlr4.Runtime.Dfa;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public enum PositionWithinArray
{
    Name,
    Value
}

/// <summary>
/// 
/// </summary>
/// <param name="Assignment"></param>
/// <param name="StartValue"></param>
/// <param name="EndValue">Exclusive</param>
/// <param name="Comma"/>
public record ArrayPropertyMeta(IToken? Assignment = null, IToken? StartValue = null, IToken? EndValue = null, IToken? Comma = null)
{
    public static readonly ArrayPropertyMeta Empty = new();
    public IToken? LastToken => Comma ?? EndValue ?? StartValue ?? Assignment;

    public string GetValue(ReadOnlySpan<char> content, int? maxValue = null)
    {
        if (StartValue is null || EndValue is null)
        {
            return string.Empty;
        }

        var end = maxValue ?? EndValue.StopIndex+1;
        return content[StartValue.StartIndex..end].ToString();
    }
}

public static class TokenListOperations
{
    public enum TokenTypeFamily
    {
        Other,
        Directive,
    }

    public static readonly FrozenSet<int> DirectiveTypes =
    [
        DOTTEXT, DOTENCODING, DOTFILL, DOTFILLWORD, DOTLOHIFILL, DOTCPU, DOTBYTE, DOTWORD, DOTDWORD, PRINT, PRINTNOW,
        VAR, DOTIMPORT, CONST, IF, ELSE, ERRORIF, EVAL, FOR, WHILE, STRUCT, DEFINE, FUNCTION, RETURN, MACRO,
        PSEUDOCOMMAND, PSEUDOPC, NAMESPACE, SEGMENT, SEGMENTDEF, SEGMENTOUT, MODIFY, FILEMODIFY, PLUGIN, LABEL, FILE,
        DISK, PC, BREAK, WATCH, ZP
    ];

    public static readonly FrozenSet<int> TextTypes =
    [
        UNQUOTED_STRING, ONLYA
    ];

    public static readonly FrozenSet<int> PropertyValueTypes =
    [
        STRING, BIN_NUMBER, HEX_NUMBER, DEC_NUMBER, UNQUOTED_STRING, TRUE, FALSE,
    ];
    
    public static bool IsTextType(this IToken token) => TextTypes.Contains(token.Type);
    public static bool IsDirectiveType(this IToken token) => DirectiveTypes.Contains(token.Type);
    public static bool IsPropertyValueType(this IToken token) => PropertyValueTypes.Contains(token.Type);

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
    /// <param name="absolutePosition">Absolute cursor position</param>
    /// <returns>Returns property name, type of position, text left of the position and entire value.</returns>
    internal static (IToken? Name, PositionWithinArray Position, string Root, string Value) 
        GetColumnPositionData(FrozenDictionary<IToken, ArrayPropertyMeta> properties, ReadOnlySpan<char> content, int absolutePosition)
    {
        // when no properties, it has to be on an empty one
        if (properties.Count == 0)
        {
            return (null, PositionWithinArray.Name, "", "");
        }

        foreach (var pair in properties.OrderBy(p => p.Key.StartIndex))
        {
            var name = pair.Key;
            var meta = pair.Value;
            var comma = meta.Comma;
            bool isWithinProperty;
            if (comma is not null)
            {
                isWithinProperty = name.StartIndex <= absolutePosition && comma.StopIndex > absolutePosition;
            }
            else
            {
                isWithinProperty = true;
            }

            if (isWithinProperty)
            {
                var assignment = meta.Assignment;
                if (assignment is null)
                {
                    return (pair.Key, PositionWithinArray.Name, content[name.StartIndex..absolutePosition].ToString(), meta.GetValue(content));
                }
                else
                {
                    if (absolutePosition < assignment.StartIndex)
                    {
                        return (name, PositionWithinArray.Name, content[name.StartIndex..absolutePosition].ToString(), meta.GetValue(content));
                    }
                    else
                    {
                        return (name, PositionWithinArray.Value, meta.GetValue(content, absolutePosition), meta.GetValue(content));
                    }
                }
            }
        }
        return (null, PositionWithinArray.Name, "", "");
    }
    
    /// <summary>
    /// Returns information about array properties.
    /// </summary>
    /// <param name="tokens">Array tokens excluding open bracket</param>
    /// <returns></returns>
    internal static FrozenDictionary<IToken, ArrayPropertyMeta> GetArrayProperties(ReadOnlySpan<IToken> tokens)
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
        while (index < tokens.Length && tokens[index].Type is not (CLOSE_BRACKET or EOL or KickAssemblerLexer.Eof))
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
        }

        return result.ToFrozenDictionary();
    }

    private enum GetArrayPropertiesState
    {
        Name,
        Assignment,
        Value,
        Comma,
    }

    /// <summary>
    /// Finds directive and optional keyword.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    internal static (int Directive, string? Option)? FindDirectiveAndOption(ReadOnlySpan<IToken> tokens)
    {
        if (tokens.IsEmpty)
        {
            return null;
        }

        var token = tokens[^1];
        if (GetTokenTypeFamily(token) == TokenTypeFamily.Directive)
        {
            return (token.Type, null);
        }

        if (tokens.Length > 1 && token.IsTextType())
        {
            var previousToken = tokens[^2];
            if (GetTokenTypeFamily(previousToken) == TokenTypeFamily.Directive)
            {
                return (previousToken.Type, token.Text);
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
    /// Finds array open bracket from the end of the tokens towards start.
    /// If array is malformed, null is returned.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    internal static int? FindWithinArrayOpenBracket(ReadOnlySpan<IToken> tokens)
    {
        if (tokens.IsEmpty)
        {
            return null;
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
                _ =>  IsPropertyValueType(current) ? FindArrayOpenBracketState.Value :  FindArrayOpenBracketState.Invalid,
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
        Assignment,
        Comma,
        PropertyName,
        OpenBracket,
    }

    internal static int? GetTokenIndexAtColumn(ReadOnlySpan<IToken> tokens, int lineStart, int column)
    {
        int position = lineStart + column;
        for (int i = 0; i < tokens.Length; i++)
        {
            var t = tokens[i];
            if (t.StartIndex > position)
            {
                break;
            }

            if (t.StopIndex >= position)
            {
                return i;
            }
        }

        return null;
    }
}