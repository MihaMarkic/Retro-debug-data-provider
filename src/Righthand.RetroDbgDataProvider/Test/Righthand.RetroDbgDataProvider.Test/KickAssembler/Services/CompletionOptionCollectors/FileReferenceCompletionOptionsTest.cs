using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class FileReferenceCompletionOptionsTest: CompletionOptionTestBase
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
        [TestCase("#import \"|", ExpectedResult = true)]
        [TestCase("#import \"xxx|", ExpectedResult = false)]
        [TestCase("#import \" \"|", ExpectedResult = false)]
        [TestCase("#importx \"|", ExpectedResult = false)]
        [TestCase("#import x \"|", ExpectedResult = false)]
        [TestCase("#importif x \"|", ExpectedResult = true)]
        public bool GivenSample_ReturnsCorrectMatchCriteria(string input)
        {
            var tc = CreateCase(input, 0);

            var actual = FileReferenceCompletionOptions.GetFileReferenceSuggestion(tc.Tokens.AsSpan(), tc.Content.AsSpan(), TextChangeTrigger.CharacterTyped);

            return actual.IsMatch;
        }
    }
    [TestFixture]
    public class GetOption : FileReferenceCompletionOptionsTest
    {
        private (int ZeroBasedColumnIndex, int TokenIndex, ImmutableArray<IToken> Tokens) GetColumnAndTokenIndex(
            string input)
        {
            int zeroBasedColumn = input.IndexOf('|');

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
        [TestCase("  #import \"src/|", ExpectedResult = true)]
        [TestCase("  #import \"src/|\"", ExpectedResult = true)]
        [TestCase("#import |\"", ExpectedResult = false)]
        [TestCase("#import x \"|", ExpectedResult = false)]
        [TestCase("#importif \"|", ExpectedResult = true)]
        [TestCase("  #importif \"|", ExpectedResult = true)]
        [TestCase("#importif |\"", ExpectedResult = false)]
        [TestCase("#importif x \"|", ExpectedResult = true)]
        [TestCase("#import \"|multi_import.asm\"", ExpectedResult = true)]
        public bool GivenTestCase_ReturnsWhetherCompletionOptionHasBeenFound(string input)
        {
            var projectServices = Substitute.For<IProjectServices>();
            projectServices.GetMatchingFiles(null!, null!, null!).ReturnsForAnyArgs(FrozenDictionary<ProjectFileKey, FrozenSet<string>>.Empty);
            projectServices.GetMatchingDirectories(null!).ReturnsForAnyArgs(FrozenDictionary<ProjectFileKey, FrozenSet<string>>.Empty);
            var context = new CompletionOptionContext(projectServices);

            var tc = CreateCase(input, 0);

            var actual = FileReferenceCompletionOptions.GetOption(tc.Tokens.AsSpan(), tc.Content.AsSpan(), TextChangeTrigger.CompletionRequested, tc.Column, "", context);

            return actual is not null;
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
        public bool CompletionRequestedTypedCases(string input)
        {
            var projectServices = Substitute.For<IProjectServices>();
            projectServices.GetMatchingFiles(null!, null!, null!).ReturnsForAnyArgs(FrozenDictionary<ProjectFileKey, FrozenSet<string>>.Empty);
            projectServices.GetMatchingDirectories(null!).ReturnsForAnyArgs(FrozenDictionary<ProjectFileKey, FrozenSet<string>>.Empty);
            var context = new CompletionOptionContext(projectServices);

            var tc = CreateCase(input, 0);

            var actual = FileReferenceCompletionOptions.GetOption(tc.Tokens.AsSpan(), tc.Content.AsSpan(), TextChangeTrigger.CharacterTyped, tc.Column, "", context);
            
            return actual is not null;
        }
    }
}