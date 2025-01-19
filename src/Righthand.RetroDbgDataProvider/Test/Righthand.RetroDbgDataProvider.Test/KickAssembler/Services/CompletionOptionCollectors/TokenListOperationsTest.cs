using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Test.KickAssembler.Grammar;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class TokenListOperationsTest
{
    private static IToken CreateToken(int tokenType) => new MockToken { Type = tokenType };
    private static IToken CreateToken(int tokenType, string text, int index = -1) => new MockToken { Type = tokenType, Text = text, TokenIndex = index };

    private static ImmutableArray<IToken> CreateTokens(params ImmutableArray<int> tokenTypes)
        => [..tokenTypes.Select(CreateToken)];

    private static ImmutableArray<IToken> CreateTokens(params ImmutableArray<(int Type, string Text)> tokenTypes)
        => [..tokenTypes.Select((t, i) => CreateToken(t.Type, t.Text, i))];

    [TestFixture]
    public class GetTokenIndexAtColumn : TokenListOperationsTest
    {
        [TestCase("[test=4,", 7, ExpectedResult = COMMA)]
        [TestCase("[test=4,", 0, ExpectedResult = OPEN_BRACKET)]
        public int? GivenSample_ReturnsActual(string input, int column)
        {
            var tokens = LexerTest.GetTokens<LexerErrorListener>(input, out _);
            var actual = TokenListOperations.GetTokenIndexAtColumn(tokens.AsSpan(), 0, column);
            if (actual.HasValue)
            {
                var token = tokens[actual.Value];

                return token.Type;
            }

            return null;
        }
    }
    [TestFixture]
    public class GetTokenIndexToTheLeftOfColumn : TokenListOperationsTest
    {
        [TestCase("[test=4,", 7, ExpectedResult = DEC_NUMBER)]
        [TestCase("[test=4,", 0, ExpectedResult = null)]
        public int? GivenSample_ReturnsActual(string input, int column)
        {
            var tokens = LexerTest.GetTokens<LexerErrorListener>(input, out _);
            var actual = TokenListOperations.GetTokenIndexToTheLeftOfColumn(tokens.AsSpan(), 0, column);
            if (actual.HasValue)
            {
                var token = tokens[actual.Value];

                return token.Type;
            }

            return null;
        }
    }

    [TestFixture]
    public class FindWithinArrayOpenBracket : TokenListOperationsTest
    {
        private static IEnumerable<(ImmutableArray<IToken> Tokens, int? ExpectedResult)> GetSource()
        {
            yield return (ImmutableArray<IToken>.Empty, null);
            yield return (CreateTokens(OPEN_STRING), null);
            yield return (CreateTokens(OPEN_BRACKET), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT, STRING), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT, UNQUOTED_STRING), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT, BIN_NUMBER), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT, DEC_NUMBER), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT, HEX_NUMBER), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, COMMA, UNQUOTED_STRING), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT,DEC_NUMBER, COMMA), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT,DEC_NUMBER, COMMA, UNQUOTED_STRING), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT,DEC_NUMBER, COMMA, UNQUOTED_STRING, ASSIGNMENT), 0);
            yield return (CreateTokens(OPEN_BRACKET, UNQUOTED_STRING, ASSIGNMENT, OPEN_STRING), 0);
        }

        [TestCaseSource(nameof(GetSource))]
        public void GivenSample_ReturnsExpectedResult((ImmutableArray<IToken> Tokens, int? ExpectedResult) td)
        {
            var actual = TokenListOperations.FindWithinArrayOpenBracket(td.Tokens.AsSpan());

            Assert.That(actual, Is.EqualTo(td.ExpectedResult));
        }
    }

    [TestFixture]
    public class FindBodyStartForArrays : TokenListOperationsTest
    {
        private static IEnumerable<(ImmutableArray<IToken> Tokens, int? ExpectedResult)> GetSource()
        {
            yield return (ImmutableArray<IToken>.Empty, null);
            yield return (CreateTokens(OPEN_BRACE), 0);
            yield return (CreateTokens(OPEN_BRACE, EOL), 0);
            yield return (CreateTokens(OPEN_BRACE, EOL, EOL), 0);
            yield return (CreateTokens(OPEN_BRACE, OPEN_BRACKET, CLOSE_BRACKET), 0);
            yield return (CreateTokens(CLOSE_BRACKET), null);
        }
        [TestCaseSource(nameof(GetSource))]
        public void GivenSample_ReturnsExpectedResult((ImmutableArray<IToken> Tokens, int? ExpectedResult) td)
        {
            var actual = TokenListOperations.FindBodyStartForArrays(td.Tokens.AsSpan());

            Assert.That(actual, Is.EqualTo(td.ExpectedResult));
        }
    }

    [TestFixture]
    public class SkipArray : TokenListOperationsTest
    {
        private static IEnumerable<(ImmutableArray<IToken> Tokens, int? ExpectedResult)> GetSource()
        {
            yield return (ImmutableArray<IToken>.Empty, null);
            yield return (CreateTokens(OPEN_BRACKET, CLOSE_BRACKET), -1);
            yield return (CreateTokens(BIN_NUMBER, OPEN_BRACKET, CLOSE_BRACKET), 0);
        }
        [TestCaseSource(nameof(GetSource))]
        public void GivenSample_ReturnsExpectedResult((ImmutableArray<IToken> Tokens, int? ExpectedResult) td)
        {
            var actual = TokenListOperations.SkipArray(td.Tokens.AsSpan(), isMandatory: true);

            Assert.That(actual, Is.EqualTo(td.ExpectedResult));
        }
    }
    [TestFixture]
    public class FindDirectiveAndOption : TokenListOperationsTest
    {
        private static IEnumerable<(ImmutableArray<IToken> Tokens, (int DirectiveTokenIndex, int? OptionTokenIndex)? ExpectedResult)> GetSource()
        {
            yield return (ImmutableArray<IToken>.Empty, null);
            yield return (CreateTokens((DISK, ".disk")), (0, null));
            yield return (CreateTokens((DOTIMPORT, ".import"), (UNQUOTED_STRING, "binary")), (0, 1));
            yield return (CreateTokens((UNQUOTED_STRING, "binary")), null);
        }
        [TestCaseSource(nameof(GetSource))]
        public void GivenSample_ReturnsExpectedResult((ImmutableArray<IToken> Tokens, (int DirectiveTokenIndex, int? OptionTokenIndex)? ExpectedResult) td)
        {
            var result = TokenListOperations.FindDirectiveAndOption(td.Tokens.AsSpan());
            (int DirectiveTokenIndex, int? OptionTokenIndex)? actual;
            if (result is not null)
            {
                actual = (result.Value.DirectiveToken.TokenIndex, result.Value.OptionToken?.TokenIndex);
            }
            else
            {
                actual = null;
            }

            Assert.That(actual, Is.EqualTo(td.ExpectedResult));
        }
    }
    
    internal record ArrayPropertyMetaBuilder(int? AssignmentIndex = null, int? StartValueIndex = null, int? EndValueIndex = null, int? CommaIndex = null)
    {
        internal static readonly ArrayPropertyMetaBuilder Empty = new();

        internal ArrayPropertyMeta Create(ImmutableArray<IToken> tokens) => new ArrayPropertyMeta(
            AssignmentIndex is not null ? tokens[AssignmentIndex.Value]: null,
            StartValueIndex is not null ? tokens[StartValueIndex.Value]: null,
            EndValueIndex is not null ? tokens[EndValueIndex.Value]: null,
            CommaIndex is not null ? tokens[CommaIndex.Value] : null
        );
    }

    [TestFixture]
    public class GetArrayProperties : TokenListOperationsTest
    {
        public record struct TestItem(ImmutableArray<IToken> Tokens, FrozenDictionary<IToken, ArrayPropertyMeta> ExpectedResult);
        private static TestItem
            CreateCase(ImmutableArray<IToken> tokens, params ImmutableArray<(int TokenIndex, ArrayPropertyMetaBuilder MetaBuilder)> items)
            => new (tokens, items.ToFrozenDictionary(i => tokens[i.TokenIndex], i => i.MetaBuilder.Create(tokens)));

        private static IEnumerable<TestItem> GetSource()
        {
            yield return new (ImmutableArray<IToken>.Empty, FrozenDictionary<IToken, ArrayPropertyMeta>.Empty);
            yield return new (CreateTokens(DISK, EOL), FrozenDictionary<IToken, ArrayPropertyMeta>.Empty);
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, EOL), (0, ArrayPropertyMetaBuilder.Empty));
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, COMMA, EOL), (0, new ArrayPropertyMetaBuilder(CommaIndex: 1)));
            yield return CreateCase(
                CreateTokens(UNQUOTED_STRING, COMMA, UNQUOTED_STRING, EOL), 
                (0, new ArrayPropertyMetaBuilder(CommaIndex: 1)), (2, ArrayPropertyMetaBuilder.Empty));
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, ASSIGNMENT, COMMA, EOL), (0, new ArrayPropertyMetaBuilder(1, CommaIndex: 2)));
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, ASSIGNMENT, STRING, COMMA, EOL), (0, new ArrayPropertyMetaBuilder(1, 2, 2, CommaIndex: 3)));
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, ASSIGNMENT, STRING, EOL), (0, new ArrayPropertyMetaBuilder(1, 2, 2)));
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, ASSIGNMENT, OPEN_STRING, EOL), (0, new ArrayPropertyMetaBuilder(1, 2, 2)));
        }

        [TestCaseSource(nameof(GetSource))]
        public void GivenSample_ReturnsExpectedResult(TestItem td)
        {
            var actual = TokenListOperations.GetArrayProperties(td.Tokens.AsSpan());

            Assert.That(actual, Is.EqualTo(td.ExpectedResult));
        }
    }

    [TestFixture]
    public class GetColumnPositionData : TokenListOperationsTest
    {
        private static ImmutableArray<IToken> GetAllTokens(string text)
        {
            var input = new AntlrInputStream(text);
            var lexer = new KickAssemblerLexer(input);
            var stream = new BufferedTokenStream(lexer);
            stream.Fill();
            var tokens = stream.GetTokens();
            return [..tokens];
        }

        private static FrozenDictionary<IToken, ArrayPropertyMeta>
            CreateProperties(ImmutableArray<IToken> tokens, params ImmutableArray<(int TokenIndex, ArrayPropertyMetaBuilder MetaBuilder)> items)
            => items.ToFrozenDictionary(i => tokens[i.TokenIndex], i => i.MetaBuilder.Create(tokens));

        public record struct TestItem(
            FrozenDictionary<IToken, ArrayPropertyMeta> Properties,
            string Content,
            int AbsolutePosition,
            (IToken? Name, PositionWithinArray Position, string Root, string Value, ArrayPropertyMeta? MatchingProperty) ExpectedResult);

        private static (FrozenDictionary<IToken, ArrayPropertyMeta> Properties, string Content, IToken Name, int Cursor) GetValues(string source, int? tokenNameIndex = null,
            int skipTokensCount = 0)
        {
            var cursor = source.IndexOf('|') - 1;
            source = source.Replace("|", "");
            var tokens = GetAllTokens(source);
            var nameToken = tokenNameIndex is not null ? tokens[tokenNameIndex.Value] : tokens.First(t => t.Text != "\"" && !string.IsNullOrWhiteSpace(t.Text));
            var properties = TokenListOperations.GetArrayProperties(tokens.AsSpan()[skipTokensCount..]);
            return (properties, source, nameToken, cursor);
        }

        private static IEnumerable<TestItem> GetSource()
        {
            {
                var (properties, source, name, cursor) = GetValues("first|");
                yield return new(properties, source, cursor, (name, PositionWithinArray.Name, "first", "", properties[name]));

                (properties, source, name, cursor) = GetValues(" alfa|");
                yield return new(properties, source, cursor, (name, PositionWithinArray.Name, "alfa", "", properties[name]));

                (properties, source, name, cursor) = GetValues(" alfa|, ");
                yield return new(properties, source, cursor, (name, PositionWithinArray.Name, "alfa", "", properties[name]));
                (properties, source, name, cursor) = GetValues(" alfa,| ");
                yield return new(properties, source, cursor, (null, PositionWithinArray.Name, "", "", null));

                (properties, source, name, cursor) = GetValues("one=|, ");
                yield return new(properties, source, cursor, (name, PositionWithinArray.Value, "", "", properties[name]));
                (properties, source, name, cursor) = GetValues("one=xx|y, ");
                yield return new(properties, source, cursor, (name, PositionWithinArray.Value, "xx", "xxy", properties[name]));
                (properties, source, name, cursor) = GetValues("pre,one=xx|y, ", 2);
                yield return new(properties, source, cursor, (name, PositionWithinArray.Value, "xx", "xxy", properties[name]));
            }
        }

        [TestCaseSource(nameof(GetSource))]
        public void GivenSample_ReturnsExpectedResult(TestItem td)
        {
            var actual = TokenListOperations.GetColumnPositionData(td.Properties, td.Content, td.AbsolutePosition);
            
            Assert.That(actual, Is.EqualTo(td.ExpectedResult));
        }
    }

    [DebuggerDisplay("{TypeText,q}")]
    public class MockToken: IToken
    {
        public string Text { get; init; } = "";
        public int Type { get; init; } = -1;
        public int Line { get; } = -1;
        public int Column { get; } = -1;
        public int Channel { get; } = -1;
        public int TokenIndex { get; init; } = -1;
        public int StartIndex { get; init; } = -1;
        public int StopIndex { get; init; } = -1;
        public ITokenSource TokenSource { get; } = null!;
        public ICharStream InputStream { get; } = null!;
        public string TypeText => KickAssemblerLexer.DefaultVocabulary.GetSymbolicName(Type);
    }
}