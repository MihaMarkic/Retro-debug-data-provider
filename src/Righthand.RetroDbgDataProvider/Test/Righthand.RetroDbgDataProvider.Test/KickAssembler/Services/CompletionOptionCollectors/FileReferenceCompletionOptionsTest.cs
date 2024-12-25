using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class FileReferenceCompletionOptionsTest
{
    [TestFixture]
    public class GetFileReferenceSuggestion : FileReferenceCompletionOptionsTest
    {
        /// <summary>
        /// | signifies the caret position.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="trigger"></param>
        /// <returns></returns>
        [TestCase("#import \"", TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        [TestCase("#import \"xxx", TextChangeTrigger.CharacterTyped, ExpectedResult = false)]
        [TestCase("#import \" \"", TextChangeTrigger.CharacterTyped, ExpectedResult = false)]
        [TestCase("#importx \"", TextChangeTrigger.CharacterTyped, ExpectedResult = false)]
        [TestCase("#import x \"", TextChangeTrigger.CharacterTyped, ExpectedResult = false)]
        [TestCase("#importif x \"", TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        public bool GivenSample_ReturnsCorrectMatchCriteria(string input, TextChangeTrigger trigger)
        {
            var tokens = AntlrTestUtils.GetAllChannelTokens(input);

            var actual = FileReferenceCompletionOptions.GetFileReferenceSuggestion(tokens.AsSpan(), input, trigger);

            return actual.IsMatch;
        }
    }
    [TestFixture]
    public class GetOption : FileReferenceCompletionOptionsTest
    {
        private (int ZeroBasedColumnIndex, int TokenIndex, ImmutableArray<IToken> Tokens) GetColumnAndTokenIndex(
            string input)
        {
            int zeroBasedColumn = input.IndexOf('|') - 1;

            var tokens = AntlrTestUtils.GetAllChannelTokens(input.Replace("|", ""));
            var token = tokens.FirstOrDefault(t => t.StartIndex <= zeroBasedColumn && t.StopIndex >= zeroBasedColumn) ??
                        tokens[^1];
            var tokenIndex = tokens.IndexOf(token);
            return (zeroBasedColumn, tokenIndex, tokens);
        }

        /// <summary>
        /// | signifies the caret position.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [TestCase("#import \"|", ExpectedResult = true)]
        [TestCase("  #import \"|", ExpectedResult = true)]
        [TestCase("#import |\"", ExpectedResult = false)]
        [TestCase("#import x \"|", ExpectedResult = false)]
        [TestCase("#importif \"|", ExpectedResult = true)]
        [TestCase("  #importif \"|", ExpectedResult = true)]
        [TestCase("#importif |\"", ExpectedResult = false)]
        [TestCase("#importif x \"|", ExpectedResult = true)]
        [TestCase("#import \"|multi_import.asm\"", ExpectedResult = true)]
        public bool CharacterTypedCases(string input)
        {
            var (zeroBasedColumn, _, tokens) = GetColumnAndTokenIndex(input);

            var actual =
                FileReferenceCompletionOptions.GetOption(tokens.AsSpan(), input.Replace("|", ""),
                    TextChangeTrigger.CharacterTyped,
                    zeroBasedColumn);

            return actual?.Type == CompletionOptionType.FileReference;
        }

        /// <summary>
        /// | signifies the caret position.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [TestCase("#import \"|", ExpectedResult = true)]
        [TestCase("  #import \"|", ExpectedResult = true)]
        [TestCase("#import |\"", ExpectedResult = false)]
        [TestCase("#import x \"|", ExpectedResult = false)]
        [TestCase("#importif \"|", ExpectedResult = true)]
        [TestCase("  #importif \"|", ExpectedResult = true)]
        [TestCase("#importif |\"", ExpectedResult = false)]
        [TestCase("#importif x \"|", ExpectedResult = true)]
        [TestCase("#import \"|multi_import.asm\"", ExpectedResult = true)]
        [TestCase("#import \"multi|_import.asm\"", ExpectedResult = true)]
        [TestCase("#import \"multi_import.as|m\"", ExpectedResult = true)]
        [TestCase("#import \"multi_import.as|", ExpectedResult = true)]
        public bool CompletionRequestedTypedCases(string input)
        {
            var (zeroBasedColumn, tokenIndex, tokens) = GetColumnAndTokenIndex(input);

            var actual =
                FileReferenceCompletionOptions.GetOption(tokens.AsSpan(), input,
                    TextChangeTrigger.CompletionRequested,
                    zeroBasedColumn);

            return actual?.Type == CompletionOptionType.FileReference;
        }
    }
}