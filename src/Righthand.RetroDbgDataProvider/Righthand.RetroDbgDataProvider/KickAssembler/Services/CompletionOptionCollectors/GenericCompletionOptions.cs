using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class GenericCompletionOptions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="lineTokens"></param>
    /// <param name="text"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineLength"></param>
    /// <param name="lineNumber">0 based line number in source file</param>
    /// <param name="column">0 based column number within selected line</param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength,
        int lineNumber, int lineCursor, CompletionOptionContext context)
    {
        Debug.WriteLine($"Trying {nameof(GenericCompletionOptions)}");
        int absoluteLineCursor = lineStart + lineCursor;
        var currentTokenIndex = lineTokens.GetTokenIndexAtColumn(0, absoluteLineCursor);
        string root = "";
        int replacementLength = 0;
        if (currentTokenIndex is not null)
        {
            var currentToken = lineTokens[currentTokenIndex.Value];
            if (currentToken.Type is (STRING or OPEN_STRING or IIF_CONDITION or IF_CONDITION))
            {
                return null;
            }
            if (currentToken.IsTextType() || currentToken.IsPreprocessorDirectiveType() || currentToken.IsDirectiveType())
            {
                string rootPrefix = "";
                var attachedTokenOnLeftIndex = TokenListOperations.GetAttachedTokenToTheLeft(lineTokens[..(currentTokenIndex.Value+1)]);
                if (attachedTokenOnLeftIndex is not null)
                {
                    var attachedTokenOnLeft = lineTokens[attachedTokenOnLeftIndex.Value];
                    rootPrefix = attachedTokenOnLeft.Type is (DOT or HASH or BANG) ? attachedTokenOnLeft.Text : "";
                }

                var partialText = currentToken.TextUpToColumn(absoluteLineCursor);
                root = $"{rootPrefix}{partialText}";
                replacementLength = rootPrefix.Length + currentToken.Length();
            }
            else if (currentToken.Type is (DOT or HASH or BANG))
            {
                root = currentToken.Text;
                replacementLength = 1;
            }
        }

        var builder = new HashSet<Suggestion>();

        Add(builder, root, SuggestionOrigin.PreprocessorDirective, PreprocessorDirectives);
        Add(builder, root, SuggestionOrigin.DirectiveOption, DirectiveProperties.AllDirectives);

        FrozenSet<Label> allUniqueLabels = [..context.ProjectServices.CollectLabels()];
        FrozenSet<string> labelNames = [..allUniqueLabels.Select(l =>  l.FullName)];
        Add(builder, root, SuggestionOrigin.Label, labelNames);

        // variables are made up by global ones and local ones within file
        var localVariables = context.SourceFile.GetLocalVariables().Where(v => v.Range!.IsInRange(lineNumber, lineCursor));
        var globalVariables = context.ProjectServices.CollectVariables().Where(v => v.VariableType == VariableType.Global);
        var variables = globalVariables.Union(localVariables);
        FrozenSet<string> variableNames = [.. variables.Select(v => v.Name)];
        Add(builder, root, SuggestionOrigin.Variable, variableNames);

        FrozenSet<string> constantNames = [.. context.ProjectServices.CollectConstants().Select(l => l.Name)];
        Add(builder, root, SuggestionOrigin.Constant, constantNames);

        FrozenSet<string> enumValueNames = [.. context.ProjectServices.CollectEnumValues().SelectMany(l => l.Values.Select(v => v.Name))];
        Add(builder, root, SuggestionOrigin.EnumValue, enumValueNames);

        FrozenSet<string> macroNames = [.. context.ProjectServices.CollectMacros().Select(m => m.Name)];
        Add(builder, root, SuggestionOrigin.Macro, macroNames);
        
        FrozenSet<string> functionNames = [.. context.ProjectServices.CollectFunctions().Select(m => m.Name)];
        Add(builder, root, SuggestionOrigin.Function, functionNames);
        
        Add(builder, root, SuggestionOrigin.Color, ColorConstants.Colors);

        Add(builder, root, SuggestionOrigin.Math, MathLibrary.FunctionNames);

        if (builder.Count > 0)
        {
            var suggestions = builder.ToFrozenSet();
            return new CompletionOption(root, replacementLength, string.Empty, string.Empty, suggestions);
        }
        return null;
    }

    private static int Add(HashSet<Suggestion> builder, string root, SuggestionOrigin suggestionOrigin, IEnumerable<string> candidates)
    {
        int count = 0;
        foreach (var c in candidates.Where(d => d.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
        {
            builder.Add(new StandardSuggestion(suggestionOrigin, c, 0));
            count++;
        }

        return count;
    }
}