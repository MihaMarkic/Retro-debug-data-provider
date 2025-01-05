using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static partial class PreprocessorDirectivesCompletionOptions
{
    internal static CompletionOption? GetOption(ReadOnlySpan<char> line, TextChangeTrigger trigger, int column)
    {
        var (isMatch, root, replaceableText) = GetPreprocessorDirectiveSuggestion(line, trigger, column);
        if (isMatch)
        {
            var suggestions = CompletionOptionCollectorsCommon.CreateSuggestionsFromTexts(root, KickAssemblerLexer.PreprocessorDirectives,
                SuggestionOrigin.PreprocessorDirective);
            return new CompletionOption(root, replaceableText.Length + 1, string.Empty, string.Empty, suggestions);
        }

        return null;
    }


    [GeneratedRegex("""
                    ^\s*#(?<import>[a-zA-Z]*)$
                    """, RegexOptions.Singleline)]
    internal static partial Regex PreprocessorDirectiveRegex();

    /// <summary>
    /// Returns possible completion for preprocessor directives.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="trigger"></param>
    /// <param name="column">Caret column</param>
    /// <returns></returns>
    internal static (bool IsMatch, string Root, string ReplaceableText) GetPreprocessorDirectiveSuggestion(
        ReadOnlySpan<char> line, TextChangeTrigger trigger, int column)
    {
        // check obvious conditions
        if (line.Length == 0 || trigger == TextChangeTrigger.CharacterTyped && line[^1] != '#')
        {
            return (false, string.Empty, string.Empty);
        }

        var match = PreprocessorDirectiveRegex().Match(line.ToString());
        if (match.Success)
        {
            int indexOfHash = line.IndexOf('#');
            string root = line[(indexOfHash + 1)..(column + 1)].ToString();
            return (true, root, match.Groups["import"].Value);
        }

        return (false, string.Empty, string.Empty);
    }
}