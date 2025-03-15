using System.Diagnostics;
using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

internal static class PreprocessorExpressionCompletionOptions
{
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int column,
        CompletionOptionContext context)
    {
        Debug.WriteLine($"Trying {nameof(PreprocessorExpressionCompletionOptions)}");
        var line = text.AsSpan()[lineStart..(lineStart + lineLength)];
        if (line.Length == 0)
        {
            return null;
        }

        var lineMeta = GetMetaInformation(lineTokens, text, lineStart, lineLength, column);
        if (lineMeta is null)
        {
            return null;
        }

        var (root, currentValue) = lineMeta.Value;
        var preprocessorSymbols = context.ProjectServices.CollectPreprocessorSymbols();
        var suggestions = CompletionOptionCollectorsCommon
            .CreateSuggestionsFromTexts(root, preprocessorSymbols, SuggestionOrigin.PropertyValue);
        return new(root, currentValue.Length, "", "", suggestions);
    }

    internal static LineMeta? GetMetaInformation(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int lineCursor)
    {
        int absoluteLineCursor = lineStart + lineCursor;
        var cursorTokenIndex = lineTokens.GetTokenIndexAtColumn(0, absoluteLineCursor);
        cursorTokenIndex ??= lineTokens.GetTokenIndexToTheLeftOfColumn(0, absoluteLineCursor);
        if (cursorTokenIndex is null)
        {
            return null;
        }

        var cursorToken = lineTokens[cursorTokenIndex.Value];
        if (cursorToken.Type is not (IF_CONDITION or IIF_CONDITION))
        {
            return null;
        }
        var expression = cursorToken.Text;
        return GetMetaFromExpression(expression, absoluteLineCursor - cursorToken.StartIndex);
    }

    internal static LineMeta GetMetaFromExpression(string expression, int column)
    {
        int nameStartIndex = -1;
        
        if (column == 0 || !expression[column-1].IsPartOfSymbolName())
        {
            nameStartIndex = column-1;
        }
        else
        {
            // fine start of symbol name index
            nameStartIndex = column - 1;
            while (nameStartIndex >= 0 && expression[nameStartIndex].IsPartOfSymbolName())
            {
                nameStartIndex--;
            }
        }

        int nameStopIndex = column;
        while (nameStopIndex < expression.Length && expression[nameStopIndex].IsPartOfSymbolName())
        {
            nameStopIndex++;
        }

        string root = expression[(nameStartIndex + 1)..column];
        string currentValue = expression[(nameStartIndex + 1)..nameStopIndex];
        return new LineMeta(root, currentValue);
    }

    /// <summary>
    /// Determines whether char is part of a symbol name or not.
    /// </summary>
    /// <param name="c"></param>
    /// <returns>True when <paramref name="c"/> is part of symbol name, false otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPartOfSymbolName(this char c) => char.IsDigit(c) || char.IsLetter(c);

    public readonly record struct LineMeta(string Root, string CurrentValue);
}