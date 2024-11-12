using System.Collections.Frozen;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Righthand.Retro.KickAssembler.PreprocessorCondition;

namespace Righthand.RetroDbgDataProvider.KickAssembler.PreprocessorCondition;

public static class PreprocessorConditionEvaluator
{
    public static bool IsDefined(FrozenSet<string> definedSymbols, string text)
    {
        var input = new AntlrInputStream(text);
        var lexer = new PreprocessorConditionLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new PreprocessorConditionParser(tokens);
        var condition = parser.condition();
        var visitor = new PreprocessorConditionVisitor(definedSymbols);

        bool result = visitor.Visit(condition);
        
        return result;
    }

    private class PreprocessorConditionVisitor : PreprocessorConditionParserBaseVisitor<bool>
    {
        private readonly FrozenSet<string> _definedSymbols;

        public PreprocessorConditionVisitor(FrozenSet<string> definedSymbols)
        {
            _definedSymbols = definedSymbols;
        }

        public override bool VisitConditionOperation(PreprocessorConditionParser.ConditionOperationContext context)
        {
            bool left = Visit(context.left);
            bool right = Visit(context.right);
            var opNode = (ITerminalNode)context.children[1]; 
            var op = opNode.Symbol.Type;

            return op switch
            {
                PreprocessorConditionLexer.OP_EQ => left == right,
                PreprocessorConditionLexer.OP_NE => left != right,
                PreprocessorConditionLexer.OP_OR => left || right,
                PreprocessorConditionLexer.OP_AND => left && right,
                _ => throw new Exception($"Unknown operator {opNode.GetText()}")
            };
        }

        public override bool VisitConditionParens(PreprocessorConditionParser.ConditionParensContext context)
        {
            return Visit(context.condition());
        }

        public override bool VisitConditionBang(PreprocessorConditionParser.ConditionBangContext context)
        {
            return !Visit(context.condition());
        }

        public override bool VisitConditionSymbol(PreprocessorConditionParser.ConditionSymbolContext context)
        {
            return _definedSymbols.Contains(context.GetText());
        }
    }
}