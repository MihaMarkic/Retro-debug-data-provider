using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static partial class PreprocessorDirectivesCompletionOptions
{
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int column, CompletionOptionContext context)
    {
        var (isMatch, root, replacementLength) = GetMetaInformation(lineTokens, text, lineStart, lineLength, column);
        if (isMatch)
        {
            var suggestions = CompletionOptionCollectorsCommon.CreateSuggestionsFromTexts(root, KickAssemblerLexer.PreprocessorDirectives,
                SuggestionOrigin.PreprocessorDirective);
            return new CompletionOption(root, replacementLength, string.Empty, string.Empty, suggestions);
        }

        return null;
    }

    
    /// <summary>
    /// Returns possible completion for preprocessor directives.
    /// </summary>
    /// <param name="lineTokens"></param>
    /// <param name="text"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineLength"></param>
    /// <param name="lineCursor"></param>
    /// <returns></returns>
    internal static (bool IsMatch, string Root, int ReplacementLength) GetMetaInformation(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int lineCursor)
    {
        int absoluteLineCursor = lineStart + lineCursor;
        var cursorTokenIndex = TokenListOperations.GetTokenIndexAtColumn(lineTokens, 0, absoluteLineCursor);
        if (cursorTokenIndex is null)
        {
            return (false, "", -1);
        }
        
        string root;
        int replacementLength;
        var currentToken = lineTokens[cursorTokenIndex.Value];
        if (currentToken.IsPreprocessorDirectiveType())
        {
            root = currentToken.TextUpToColumn(absoluteLineCursor);
            replacementLength = currentToken.Length();
        }
        else if (currentToken.Type == HASH)
        {
            
        }

        return (false, "", 0);
    }
}