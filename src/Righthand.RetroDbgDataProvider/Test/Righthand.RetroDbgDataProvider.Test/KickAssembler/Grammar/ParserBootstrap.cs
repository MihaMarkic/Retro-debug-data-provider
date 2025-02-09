using System.Diagnostics;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Grammar;

public abstract class ParserBootstrap<T> : BaseTest<T>
    where T : class
{
    protected KickAssemblerParserListener Run<TContext, TParserErrorListener>(string text, Func<KickAssemblerParser, TContext> run,
        out TParserErrorListener errors, params string[] defineSymbols)
        where TContext : ParserRuleContext
        where TParserErrorListener : BaseErrorListener, new()
    {
        return Run<KickAssemblerParserListener, TParserErrorListener, TContext>(text, run, out errors);
    }

    protected TListener Run<TListener, TParserErrorListener, TContext>(
        string text,
        Func<KickAssemblerParser, TContext> run, 
        out TParserErrorListener errors, 
        params string[] defineSymbols)
        where TListener : KickAssemblerParserBaseListener, new()
        where TParserErrorListener : BaseErrorListener, new()
        where TContext : ParserRuleContext
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input);
        foreach (string ds in defineSymbols)
        {
            lexer.DefinedSymbols.Add(ds);
        }

        lexer.AddErrorListener(new LexerErrorListener());
        var tokensStream = new CommonTokenStream(lexer);
        var parser = new KickAssemblerParser(tokensStream)
        {
            BuildParseTree = true
        };
        errors = new ();
        parser.AddErrorListener(errors);
        try
        {
            var tree = run(parser);
            var listener = new TListener();
            ParseTreeWalker.Default.Walk(listener, tree);
            return listener;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            var tokens = lexer.GetAllTokens();
            throw;
        }
    }

    public void Run<TContext, TParserErrorListener>(IKickAssemblerParserListener listener, string text,
        Func<KickAssemblerParser, TContext> run, out TParserErrorListener errors)
        where TParserErrorListener : BaseErrorListener, new()
        where TContext : ParserRuleContext
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input);
        lexer.AddErrorListener(new LexerErrorListener());
        var tokens = new CommonTokenStream(lexer);
        var parser = new KickAssemblerParser(tokens)
        {
            BuildParseTree = true
        };
        errors = new();
        parser.AddErrorListener(errors);
        var tree = run(parser);
        ParseTreeWalker.Default.Walk(listener, tree);
    }
}

public class ErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
        int charPositionInLine, string msg, RecognitionException e)
    {
        throw new Exception(msg, e);
    }
}

public class LexerErrorListener : IAntlrErrorListener<int>
{
    public bool ThrowOnError { get; init; } = true;
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
        int charPositionInLine, string msg, RecognitionException e)
    {
        if (ThrowOnError)
        {
            throw new Exception(msg, e);
        }
    }
}