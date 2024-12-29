using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
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
    private static IToken CreateToken(int tokenType, string text) => new MockToken { Type = tokenType, Text = text };

    private static ImmutableArray<IToken> CreateTokens(params ImmutableArray<int> tokenTypes)
        => [..tokenTypes.Select(CreateToken)];

    private static ImmutableArray<IToken> CreateTokens(params ImmutableArray<(int Type, string Text)> tokenTypes)
        => [..tokenTypes.Select(t => CreateToken(t.Type, t.Text))];

    [TestFixture]
    public class GetTokenAtColumn : TokenListOperationsTest
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
    public class FindWithinArrayOpenBracket : TokenListOperationsTest
    {
        private static IEnumerable<(ImmutableArray<IToken> Tokens, int? ExpectedResult)> GetSource()
        {
            yield return (ImmutableArray<IToken>.Empty, null);
            yield return (CreateTokens(DOUBLE_QUOTE), null);
            yield return (CreateTokens(OPEN_BRACKET), 0);
            yield return (CreateTokens(DOUBLE_QUOTE, OPEN_BRACKET), 1);
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
        private static IEnumerable<(ImmutableArray<IToken> Tokens, (int Directive, string? Option)? ExpectedResult)> GetSource()
        {
            yield return (ImmutableArray<IToken>.Empty, null);
            yield return (CreateTokens((DISK, ".disk")), (DISK, null));
            yield return (CreateTokens((DOTIMPORT, ".import"), (UNQUOTED_STRING, "binary")), (DOTIMPORT, "binary"));
            yield return (CreateTokens((UNQUOTED_STRING, "binary")), null);
        }
        [TestCaseSource(nameof(GetSource))]
        public void GivenSample_ReturnsExpectedResult((ImmutableArray<IToken> Tokens, (int Directive, string? Option)? ExpectedResult) td)
        {
            var actual = TokenListOperations.FindDirectiveAndOption(td.Tokens.AsSpan());

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
        private static (ImmutableArray<IToken> Tokens, FrozenDictionary<IToken, ArrayPropertyMeta> PropertiesMeta)
            CreateCase(ImmutableArray<IToken> tokens, params ImmutableArray<(int TokenIndex, ArrayPropertyMetaBuilder MetaBuilder)> items)
            => (tokens, items.ToFrozenDictionary(i => tokens[i.TokenIndex], i => i.MetaBuilder.Create(tokens)));

        private static IEnumerable<(ImmutableArray<IToken> Tokens, FrozenDictionary<IToken, ArrayPropertyMeta> PropertiesMeta)> GetSource()
        {
            yield return (ImmutableArray<IToken>.Empty, FrozenDictionary<IToken, ArrayPropertyMeta>.Empty);
            yield return (CreateTokens(DISK), FrozenDictionary<IToken, ArrayPropertyMeta>.Empty);
            yield return CreateCase(CreateTokens(UNQUOTED_STRING), (0, ArrayPropertyMetaBuilder.Empty));
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, COMMA), (0, new ArrayPropertyMetaBuilder(CommaIndex: 1)));
            yield return CreateCase(
                CreateTokens(UNQUOTED_STRING, COMMA, UNQUOTED_STRING), 
                (0, new ArrayPropertyMetaBuilder(CommaIndex: 1)), (2, ArrayPropertyMetaBuilder.Empty));
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, ASSIGNMENT, COMMA), (0, new ArrayPropertyMetaBuilder(1, CommaIndex: 2)));
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, ASSIGNMENT, STRING, COMMA), (0, new ArrayPropertyMetaBuilder(1, 2, 2, CommaIndex: 3)));
            yield return CreateCase(CreateTokens(UNQUOTED_STRING, ASSIGNMENT, STRING), (0, new ArrayPropertyMetaBuilder(1, 2, 2)));
        }

        [TestCaseSource(nameof(GetSource))]
        public void GivenSample_ReturnsExpectedResult(
            (ImmutableArray<IToken> Tokens, FrozenDictionary<IToken, ArrayPropertyMeta> ExpectedResult) td)
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

        internal class TokenValueBuilder
        {
            private readonly TokenBuilder _tokenBuilder;
            private readonly IToken _name;
            private readonly List<IToken> _tokens = new ();
            private bool _isAssignment;

            internal TokenValueBuilder(TokenBuilder tokenBuilder, string name)
            {
                _tokenBuilder = tokenBuilder;
                _name = _tokenBuilder.AddAny(name, UNQUOTED_STRING);
            }

            internal TokenValueBuilder AddSpace(int length)
            {
                _tokenBuilder.AddSpace(length);
                return this;
            }

            internal TokenValueBuilder AddAssignment()
            {
                _tokenBuilder.AddAny("=", ASSIGNMENT);
                _isAssignment = true;
                return this;
            }

            internal TokenValueBuilder AddValue(string text, int type)
            {
                var token = _tokenBuilder.AddAny(text, type);
                _tokens.Add(token);
                return this;
            }

            internal TokenBuilder Commit()
            {
                _tokenBuilder.Store(_name, ArrayPropertyMeta.Empty);
                return _tokenBuilder;
            }
        }
        internal class TokenBuilder
        {
            private readonly StringBuilder _stringBuilder = new();
            private readonly List<IToken> _tokens = new ();
            private readonly Dictionary<IToken, ArrayPropertyMeta> _properties = new();

            internal TokenValueBuilder AddName(string name)
            {
                return new TokenValueBuilder(this, name);
            }

            internal TokenBuilder AddSpace(int length)
            {
                _stringBuilder.Append(new string[length]);
                return this;
            }

            internal TokenBuilder Store(IToken token, ArrayPropertyMeta meta)
            {
                _properties.Add(token, ArrayPropertyMeta.Empty);
                return this;
            }

            internal IToken AddAny(string text, int type)
            {
                var token = new MockToken
                {
                    Type = UNQUOTED_STRING,
                    StartIndex = _stringBuilder.Length,
                    StopIndex = _stringBuilder.Length + text.Length,
                    Text = text,
                };
                _tokens.Add(token);
                _properties.Add(token, ArrayPropertyMeta.Empty);
                _stringBuilder.Append(text);
                return token;
            }

            public (string Content, FrozenDictionary<IToken, ArrayPropertyMeta> Meta, ImmutableArray<IToken> Tokens) Build()
                => (_stringBuilder.ToString(), _properties.ToFrozenDictionary(), _tokens.ToImmutableArray());
        }

        private static IEnumerable<(FrozenDictionary<IToken, ArrayPropertyMeta> Properties, string Content, int AbsolutePosition, 
            (IToken? Name, PositionWithinArray Position, string Root, string Value) ExpectedResult)> GetSource()
        {
            {
                // var (content, meta, tokens) = new TokenBuilder()
                //     .AddName("first").AddSpace(1).Commit()
                //     .Build();
                const string source = "first";
                var tokens = GetAllTokens(source);
                var properties = TokenListOperations.GetArrayProperties(tokens.AsSpan());
                
                yield return (properties, source, 5, (tokens[0], PositionWithinArray.Name, "first", ""));
            }
        }

        [TestCaseSource(nameof(GetSource))]
        public void GivenSample_ReturnsExpectedResult((FrozenDictionary<IToken, ArrayPropertyMeta> Properties, string Content, int AbsolutePosition,
            (IToken? Name, PositionWithinArray Position, string Root, string Value) ExpectedResult) td)
        {
            var actual = TokenListOperations.GetColumnPositionData(td.Properties, td.Content, td.AbsolutePosition);
            
            Assert.That(actual, Is.EqualTo(td.ExpectedResult));
        }
    }

    [DebuggerDisplay("{TypeText,q}")]
    private class MockToken: IToken
    {
        public string Text { get; init; } = "";
        public int Type { get; init; } = -1;
        public int Line { get; } = -1;
        public int Column { get; } = -1;
        public int Channel { get; } = -1;
        public int TokenIndex { get; } = -1;
        public int StartIndex { get; init; } = -1;
        public int StopIndex { get; init; } = -1;
        public ITokenSource TokenSource { get; } = null!;
        public ICharStream InputStream { get; } = null!;
        public string TypeText => KickAssemblerLexer.DefaultVocabulary.GetSymbolicName(Type);
    }
}