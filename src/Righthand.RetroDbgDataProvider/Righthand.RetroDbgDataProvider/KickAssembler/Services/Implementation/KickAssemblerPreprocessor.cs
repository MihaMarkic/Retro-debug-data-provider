using System.Collections.Frozen;
using Antlr4.Runtime;
using Microsoft.Extensions.Logging;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

public class FilteredTokenStream : CommonTokenStream
{
    public FilteredTokenStream(IList<IToken> tokens, ITokenSource tokenSource) : base(tokenSource)
    {
        this.tokens = tokens; 
        fetchedEOF = true;
    }

    // public FilteredTokenStream(IList<IToken> tokens,ITokenSource tokenSource, int channel) : base(tokenSource, channel)
    // {
    //     this.tokens = tokens;
    //     fetchedEOF = true;
    // }
}
/// <summary>
/// Preprocesses #ifs and #defines
/// </summary>
public class KickAssemblerPreprocessor
{
    private enum State
    {
        None,
        Hash,
        If,
        Elif,
    }

    private readonly ILogger<KickAssemblerPreprocessor> _logger;

    public KickAssemblerPreprocessor(ILogger<KickAssemblerPreprocessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Removes tokens within undefined region 
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="defines"></param>
    internal CommonTokenStream FilterUndefined(CommonTokenStream tokens, FrozenSet<string> defines)
    {
        var rewriter = new TokenStreamRewriter(tokens);
        var undefinedRanges = ExtractUndefinedRanges(tokens, defines);
        if (!undefinedRanges.IsEmpty)
        {
            var filteredStream = RemoveTokenRanges(tokens, undefinedRanges);
            return filteredStream;
        }

        return tokens;
    }

    internal FilteredTokenStream RemoveTokenRanges(CommonTokenStream tokens, ImmutableArray<Range> ranges)
    {
        var allTokens = tokens.GetTokens().ToImmutableArray();
        foreach (var range in ranges.Reverse())
        {
            int start = allTokens.IndexOf(range.From);
            int end = allTokens.IndexOf(range.To);
            allTokens = allTokens.RemoveRange(start, end - start);
        }

        return new FilteredTokenStream(allTokens, tokens.TokenSource);
    }

    internal ImmutableArray<Range> ExtractUndefinedRanges(ITokenStream tokens, FrozenSet<string> defines)
    {
        int depth = 0;
        IToken? startRange = null;
        State state = State.None;
        List<Range> undefinedRanges = new ();

        int i = 0;
        while (i < tokens.Size)
        {
            var token = tokens.Get(i);
            var tokenType = token.Type;
            if (startRange is null)
            {
                switch (state)
                {
                    case State.None:
                        if (tokenType == KickAssemblerLexer.HASH)
                        {
                            state = State.Hash;
                        }
                        break;
                    case State.Hash:
                        switch (tokenType)
                        {
                            case KickAssemblerLexer.IF:
                                state = State.If;
                                break;
                            case KickAssemblerLexer.ELIF:
                                state = State.Elif;
                                break;
                            case KickAssemblerLexer.ELSE:
                                if (i + 1 < tokens.Size)
                                {
                                    startRange = tokens.Get(i + 1);
                                    state = State.None;
                                }
                                break;
                            case KickAssemblerLexer.ENDIF:
                                if (depth > 0)
                                {
                                    depth--;
                                    state = State.None;
                                }
                                break;
                            default:
                                state = State.None;
                                break;
                        }
                        break;
                    case State.If:
                    case State.Elif:
                        if (tokenType == KickAssemblerLexer.UNQUOTED_STRING)
                        {
                            if (defines.Contains(token.Text))
                            {
                                depth++;
                                state = State.None;
                            }
                            else
                            {
                                if (i + 1 < tokens.Size)
                                {
                                    startRange = tokens.Get(i + 1);
                                    state = State.None;
                                }
                            }
                        }
                        break;
                }
            }
            // startRange is not null -> skip until #else or #endif
            else 
            {
                switch (state)
                {
                    case State.None:
                        switch (tokenType)
                        {
                            case KickAssemblerLexer.HASH:
                                state = State.Hash;
                                break;
                        }

                        break;
                    case State.Hash:
                        switch (tokenType)
                        {
                            case KickAssemblerLexer.ELSE:
                                state = State.None;
                                depth++;
                                undefinedRanges.Add(new Range(startRange, tokens.Get(i - 2)));
                                startRange = null;
                                break;
                            case KickAssemblerLexer.ELIF:
                                state = State.Elif;
                                undefinedRanges.Add(new Range(startRange, tokens.Get(i - 2)));
                                startRange = null;
                                break;
                            case KickAssemblerLexer.ENDIF:
                                state = State.None;
                                undefinedRanges.Add(new Range(startRange, tokens.Get(i - 2)));
                                startRange = null;
                                break;
                            default:
                                state = State.None;
                                break;
                        }

                        break;
                }
            }

            i++;
        }

        return [..undefinedRanges];
    }
}

internal record struct Range(IToken From, IToken To);