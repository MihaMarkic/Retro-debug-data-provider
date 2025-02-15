using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;
using NSubstitute;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public record struct GetOptionTestCase(ImmutableArray<IToken> Tokens, string Content, int Start, int End, int Column,
    CompletionOption? ExpectedResult);
public abstract class CompletionOptionTestBase
{
    protected static CompletionOptionContext NoOpContext { get; }

    static CompletionOptionTestBase()
    {
        var projectServices = Substitute.For<IProjectServices>();
        projectServices.CollectLabels().ReturnsForAnyArgs([]);
        projectServices.CollectVariables().ReturnsForAnyArgs([]);
        projectServices.CollectConstants().ReturnsForAnyArgs([]);
        projectServices.CollectEnumValues().ReturnsForAnyArgs([]);
        projectServices.CollectMacros().ReturnsForAnyArgs([]);
        projectServices.CollectFunctions().ReturnsForAnyArgs([]);
        NoOpContext = new CompletionOptionContext(projectServices, Substitute.For<IParsedSourceFile>());
    }

    private static ImmutableArray<IToken> GetAllTokens(string text)
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input);
        var stream = new BufferedTokenStream(lexer);
        stream.Fill();
        var tokens = stream.GetTokens().Where(t => t.Channel == 0);
        return [..tokens];
    }

    protected static GetOptionTestCase CreateCase(string text, int start, CompletionOption? expectedResult = null)
    {
        Debug.Assert(text.Count(c => c == '|') == 1, "Exactly one cursor | is allowed within text");
        int cursor = text.IndexOf('|');
        text = text.Replace("|", "");
        return new GetOptionTestCase(GetAllTokens(text), text, start, text.Length, cursor, expectedResult);
    }
}