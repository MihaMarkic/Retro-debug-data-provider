using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Grammar;

public abstract class Bootstrap<T>: BaseTest<T>
    where T : class
{
    protected void Run<TContext>(string text, Func<KickAssemblerParser, TContext> run)
        where TContext : ParserRuleContext
    {
        Run<KickAssemblerParserBaseListener, TContext>(text, run);
    }
    public TListener Run<TListener, TContext>(string text, Func<KickAssemblerParser, TContext> run)
    where TListener : KickAssemblerParserBaseListener, new()
    where TContext : ParserRuleContext
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input);
        lexer.AddErrorListener(new SyntaxErrorListener());
        var tokens = new CommonTokenStream(lexer);
        var parser = new KickAssemblerParser(tokens)
        {
            BuildParseTree = true
        };
        parser.AddErrorListener(new ErrorListener());
        var tree = run(parser);
        var listener = new TListener();
        ParseTreeWalker.Default.Walk(listener, tree);
        return listener;
    }
    public void Run<TContext>(IKickAssemblerParserListener listener, string text, Func<KickAssemblerParser, TContext> run)
        where TContext : ParserRuleContext
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input);
        lexer.AddErrorListener(new SyntaxErrorListener());
        var tokens = new CommonTokenStream(lexer);
        var parser = new KickAssemblerParser(tokens)
        {
            BuildParseTree = true
        };
        parser.AddErrorListener(new ErrorListener());
        var tree = run(parser);
        ParseTreeWalker.Default.Walk(listener, tree);
    }
}

public class ErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new Exception(msg, e);
    }
}

public class SyntaxErrorListener : IAntlrErrorListener<int>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new Exception(msg, e);
    }
}
