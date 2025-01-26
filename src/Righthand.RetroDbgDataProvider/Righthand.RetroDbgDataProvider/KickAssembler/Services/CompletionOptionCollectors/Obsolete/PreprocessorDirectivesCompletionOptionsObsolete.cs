using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

/// <summary>
/// Handles only #..if EXPRESSION.
/// </summary>
/// <remarks>Keyword is handled by <see cref="GenericCompletionOptions"/>, files are handled by <see cref="FileReferenceCompletionOptions"/>.</remarks>
public static partial class PreprocessorDirectivesCompletionOptionsObsolete
{
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int column, CompletionOptionContext context)
    {
        var lineMeta = GetMetaInformation(lineTokens, text, lineStart, lineLength, column);
        if (lineMeta is null)
        {
            return null;
        }

        var (positionType, directive, root, currentValue, replacementLength, hasEndDelimiter) = lineMeta.Value;
        switch (positionType)
        {
            case PositionType.Expression:
                if (directive is ".importif" or ".if" or ".elif")
                {
                    var preprocessorSymbols = context.ProjectServices.CollectPreprocessorSymbols();
                    var suggestions = CompletionOptionCollectorsCommon
                        .CreateSuggestionsFromTexts(root, preprocessorSymbols, SuggestionOrigin.PropertyValue);
                    return new(root, replacementLength, "", "", suggestions);
                }
                break;
        }

        return null;
    }

    
    /// <summary>
    /// Returns possible completion for preprocessor directives. Preprocessor directive keyword is handled by <see cref="GenericCompletionOptions"/>.
    /// </summary>
    /// <param name="lineTokens"></param>
    /// <param name="text"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineLength"></param>
    /// <param name="lineCursor"></param>
    /// <returns></returns>
    internal static LineMeta? GetMetaInformation(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int lineCursor)
    {
        if (lineTokens.IsEmpty)
        {
            return null;
        }
        int absoluteLineCursor = lineStart + lineCursor;
        var cursorTokenIndex = lineTokens.GetTokenIndexAtColumn(0, absoluteLineCursor);
        var firstValidTokenIndex = cursorTokenIndex ?? lineTokens.GetTokenIndexToTheLeftOfColumn(lineStart, absoluteLineCursor);
        if (firstValidTokenIndex is null)
        {
            return null;
        }
        
        if (cursorTokenIndex is not null && IsPreprocessorDirectiveType(lineTokens[..(firstValidTokenIndex.Value+1)]))
        {
            // leave keywords to GenericCompletionOption
            return null;
        }

        int index = firstValidTokenIndex.Value;
        while (index >= 0 && !lineTokens[index].IsPreprocessorDirectiveType())
        {
            index--;
        }

        if (index < 0)
        {
            // couldn't find preprocessor directive
            return null;
        }
        
        var keywordToken = lineTokens[index];
        var currentToken = cursorTokenIndex is not null ? lineTokens[cursorTokenIndex.Value] : null;
        var firstValidToken = lineTokens[firstValidTokenIndex.Value];

        if (currentToken is null)
        {
            // meaning cursor is positioned in space for expression
            return new(PositionType.Expression, keywordToken.Text, "", "", 0, false);
        }
        
        if (currentToken.Type is OPEN_STRING or STRING)
        {
            if (currentToken.StartIndex == absoluteLineCursor)
            {
                // don't return when cursor is in front of "  
                return null;
            }
            var hasEndDelimiter = currentToken.Type == STRING;
            var currentValue = currentToken.Type == STRING ? currentToken.Text[1..^1] : currentToken.Text[1..];
            var root = currentToken.TextUpToColumn(absoluteLineCursor)[1..];
            var replacementLength = currentValue.Length;
            return new(PositionType.Text, keywordToken.Text, root, currentValue, replacementLength, hasEndDelimiter);
        }
        else
        {
            string currentValue;
            string root;
            if (currentToken.IsTextType())
            {
                currentValue = currentToken.Text;
                root = currentToken.TextUpToColumn(absoluteLineCursor);
            }
            else
            {
                currentValue = "";
                root = "";
            }

            return new(PositionType.Expression, keywordToken.Text, root, currentValue, currentValue.Length, false);
        }
    }

    /// <summary>
    /// Checks whether cursor is at preprocessor directive keyword or candidate
    /// </summary>
    /// <param name="lineTokens"></param>
    /// <returns></returns>
    private static bool IsPreprocessorDirectiveType(ReadOnlySpan<IToken> lineTokens)
    {
        var currentToken = lineTokens[^1];
        if (currentToken.Type == HASH || currentToken.IsPreprocessorDirectiveType())
        {
            return true;
        }

        if (currentToken.IsTextType())
        {
            var previousToken = lineTokens.Length > 1 ? lineTokens[^2] : null;
            if (previousToken?.Type == HASH)
            {
                return true;
            }
        }

        return false;
    }
    public enum PositionType
    {
        Expression,
        Text
    }
    /// <summary>
    /// Holds information about line.
    /// </summary>
    /// <param name="PositionType"></param>
    /// <param name="Directive"></param>
    /// <param name="Root"></param>
    /// <param name="CurrentValue"></param>
    /// <param name="ReplacementLength"></param>
    /// <param name="HasEndDelimiter"></param>
    public record struct LineMeta(
        PositionType PositionType,
        string Directive,
        string Root,
        string CurrentValue,
        int ReplacementLength,
        bool HasEndDelimiter);
}