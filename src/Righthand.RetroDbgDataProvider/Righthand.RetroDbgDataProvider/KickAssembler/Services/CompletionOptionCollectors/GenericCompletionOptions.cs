﻿using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class GenericCompletionOptions
{
    internal static CompletionOption? GetOption(ReadOnlySpan<IToken> lineTokens, string text, int lineStart, int lineLength, int lineCursor, CompletionOptionContext context)
    {
        Debug.WriteLine($"Trying {nameof(GenericCompletionOptions)}");
        int absoluteLineCursor = lineStart + lineCursor;
        var currentTokenIndex = TokenListOperations.GetTokenIndexAtColumn(lineTokens, 0, absoluteLineCursor);
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
                    rootPrefix = attachedTokenOnLeft.Type is (DOT or HASH) ? attachedTokenOnLeft.Text : "";
                }

                var partialText = currentToken.TextUpToColumn(absoluteLineCursor);
                root = $"{rootPrefix}{partialText}";
                replacementLength = rootPrefix.Length + currentToken.Length();
            }
            else if (currentToken.Type is (DOT or HASH))
            {
                root = currentToken.Text;
                replacementLength = 1;
            }
        }

        var builder = new HashSet<Suggestion>();

        Add(builder, root, SuggestionOrigin.PreprocessorDirective, PreprocessorDirectives);
        Add(builder, root, SuggestionOrigin.DirectiveOption, DirectiveProperties.AllDirectives);
        
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