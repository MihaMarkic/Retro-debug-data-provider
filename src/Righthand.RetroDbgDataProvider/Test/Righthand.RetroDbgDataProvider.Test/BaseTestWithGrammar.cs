using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Test;

public static class AntlrTestUtils
{
    /// <summary>
    /// Returns all channel tokens.
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public static ImmutableArray<IToken> GetAllChannelTokens(string code)
    {
        var input = new AntlrInputStream(code);
        var lexer = new KickAssemblerLexer(input);
        var stream = new BufferedTokenStream(lexer);
        stream.Fill();
        return [..stream.GetTokens()];
    }

    public static FrozenDictionary<int, ImmutableArray<IToken>> GetAllChannelTokensByLineMap(string code)
    {
        return GetAllChannelTokens(code)
            .GroupBy(t => t.Line - 1)
            .ToFrozenDictionary(g => g.Key, g => g.OrderBy(t => t.Column).ToImmutableArray());
    }
}