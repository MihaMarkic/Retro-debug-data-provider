using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;
namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Grammar;

[TestFixture]
public class LexerTest
{
    public static ImmutableArray<IToken> GetTokens<TLexerErrorListener>(string text, out TLexerErrorListener errorListener, params string[] definitions)
        where TLexerErrorListener: IAntlrErrorListener<int>, new ()
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input)
        {
            DefinedSymbols = definitions.ToHashSet(),
        };
        errorListener = new TLexerErrorListener();
        lexer.AddErrorListener(errorListener);
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        var tokens = stream.GetTokens();
        return [..tokens.Where(t => t.Channel == 0)];
    }
    ImmutableArray<IToken> GetAllTokens(string text, params string[] definitions)
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input)
        {
            DefinedSymbols = definitions.ToHashSet(),
        };
        var stream = new BufferedTokenStream(lexer);
        stream.Fill();
        var tokens = stream.GetTokens();
        return [..tokens];
    }
    KickAssemblerLexer GetLexer<TLexer>(string text, params string[] definitions)
        where TLexer: IAntlrErrorListener<int>
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input)
        {
            DefinedSymbols = definitions.ToHashSet(),
        };
        lexer.AddErrorListener(new LexerErrorListener());
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        var tokens = stream.GetTokens();
        return lexer;
    }

    ImmutableArray<int> GetTokenTypes(params int[] tokens)
    {
        return [..tokens];
    }

    [TestFixture]
    public class SpecialCases : LexerTest
    {
        private static IEnumerable<(string Input, ImmutableArray<int> ExpectedTokens, string? Message)> GetTestCases()
        {
            yield return new("\"", [OPEN_STRING], "Given double quotes returns DOUBLE_QUOTE");
        }

        [TestCaseSource(nameof(GetTestCases))]
        public void GivenInput_AssertsExpected((string Input, ImmutableArray<int> ExpectedTokens, string? Message) td)
        {
            var actual  = GetTokens<LexerErrorListener>(td.Input, out _)
                .Select(t => t.Type);
            
            Assert.That(actual, Is.EquivalentTo(td.ExpectedTokens.Add(KickAssemblerLexer.Eof)), td.Message);
        }
    }
    [TestFixture]
    public class HashIf : LexerTest
    {
        [Test]
        public void WhenMoreCharsWithoutSpace_ShouldNotRecognize()
        {
            const string input = """
                                 #ifxxxx
                                 """;

            Assert.That(() => GetTokens<LexerErrorListener>(input, out _, "DEFINED"),
                Throws.Exception.With.InnerException.TypeOf<LexerNoViableAltException>());
        }

        [Test]
        public void WhenSimpleIfWithOutsideDefinedCondition_ReturnsAllTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _, "DEFINED");

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                LDA, HASH, DEC_NUMBER,
                HASHENDIF,
                KickAssemblerLexer.Eof
                );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenComplexValidIfCondition_ReturnsAllTokens()
        {
            const string input = """
                                 #if DEFINED && OTHERDEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _, "DEFINED", "OTHERDEFINED");

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                LDA, HASH, DEC_NUMBER,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenComplexInvalidIfCondition_ReturnsAllTokens()
        {
            const string input = """
                                 #if DEFINED && OTHERUNDEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _, "DEFINED");

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfElseWithIfDefinedCondition_ReturnsIfTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #else
                                    ldy #8
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _, "DEFINED");

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                    LDA, HASH, DEC_NUMBER,
                HASHELSE,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfElseWithIfUndefinedCondition_ReturnsElseTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #else
                                    ldy #8
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                HASHELSE,
                    LDY, HASH, DEC_NUMBER,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfElifWithBothUndefinedCondition_ReturnsFilteredTokens()
        {
            const string input = """
                                 #if UNDEFINED
                                    lda #5
                                 #elif UNDEFINED
                                    ldy #8
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                HASHELIF, IF_CONDITION,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenNoneOfElifAreDefined_ReturnsElseTokens()
        {
            const string input = """
                                 #if UNDEFINED
                                    lda #5
                                 #elif UNDEFINED
                                    ldy #8
                                 #else
                                    ldx #1
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                HASHELIF, IF_CONDITION,
                HASHELSE,
                    LDX, HASH, DEC_NUMBER,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenFirstIfIsDefined_ReturnsAllPreprocessTokensElseTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #elif UNDEFINED
                                    ldy #8
                                 #else
                                    ldx #1
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _, "DEFINED");

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                    LDA, HASH, DEC_NUMBER,
                HASHELIF, IF_CONDITION,
                HASHELSE,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenNestedIfElseWithIfUndefinedCondition_ReturnsElseTokens()
        {
            const string input = """
                                 #if UNDEFINED
                                    lda #5
                                 #else
                                    #if DEFINED
                                        ldy #8
                                    #endif
                                 #endif
                                 """;

            var actual = GetTokens<LexerErrorListener>(input, out _, "DEFINED");

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                HASHELSE,
                    HASHIF, IF_CONDITION,
                        LDY, HASH, DEC_NUMBER,
                    HASHENDIF,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenNestedIfElseWithIfDynamicallyDefinedCondition_ReturnsElseTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #else
                                    #define DEFINED2
                                    #if DEFINED2
                                        ldy #8
                                    #endif
                                 #endif
                                 """;

            var actual = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                HASHELSE,
                    HASHDEFINE, DEFINED_TOKEN,
                    HASHIF, IF_CONDITION,
                        LDY, HASH, DEC_NUMBER,
                    HASHENDIF,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfWithInsideDefinedCondition_ReturnsAllTokens()
        {
            const string input = """
                                 #define DEFINED
                                 #if DEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                HASHDEFINE, DEFINED_TOKEN,
                HASHIF, IF_CONDITION,
                    LDA, HASH, DEC_NUMBER,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfWithInsideUndefinedCondition_ReturnsFilteredTokens()
        {
            const string input = """
                                 #undef DEFINED
                                 #if DEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                HASHUNDEF, UNDEFINED_TOKEN,
                HASHIF, IF_CONDITION,
                    //LDA, HASH, DEC_NUMBER, EOL,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfWithUndefinedCondition_ReturnsFilteredTokens()
        {
            const string input = """
                                 #if UNDEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenNestedIfWithUndefinedCondition_ReturnsFilteredTokens()
        {
            const string input = """
                                 #if DEFINED
                                    #if UNDEFINED
                                        lda #5
                                    #endif
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _, "DEFINED");

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                    HASHIF, IF_CONDITION,
                    HASHENDIF,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenNestedIfWithDefinedCondition_ReturnsFullTokens()
        {
            const string input = """
                                 #if DEFINED
                                    #if UNDEFINED
                                        lda #5
                                    #endif
                                 #endif
                                 """;
            
            var actual  = GetTokens<LexerErrorListener>(input, out _, "DEFINED", "UNDEFINED");

            var expected = GetTokenTypes(
                HASHIF, IF_CONDITION,
                    HASHIF, IF_CONDITION,
                        LDA, HASH, DEC_NUMBER,
                    HASHENDIF,
                HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenDoesNotHaveImportOnce_ImportOnceIsFalse()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetLexer<LexerErrorListener>(input, "DEFINED").IsImportOnce;

            Assert.That(actual, Is.False);
        }
        [Test]
        public void WhenHasImportOnce_ImportOnceIsTrue()
        {
            const string input = """
                                 #importonce
                                 """;

            var l = GetLexer<LexerErrorListener>(input);
            var actual  = GetLexer<LexerErrorListener>(input).IsImportOnce;

            Assert.That(actual, Is.True);
        }
    }

    [TestFixture]
    public class HashElse : LexerTest
    {
        [Test]
        public void WhenElseContentIgnoredAndHasAdditionalChars_ShouldNotRecognize()
        {
            const string input = """
                                 #if DEFINED
                                 #elsexxx
                                 #endif
                                 """;

            Assert.That(() => GetTokens<LexerErrorListener>(input, out _, "DEFINED"),
                Throws.Exception.With.InnerException.TypeOf<LexerNoViableAltException>());
        }
        [Test]
        public void WhenElseContentIsNotIgnoredAndHasAdditionalChars_ShouldNotRecognize()
        {
            const string input = """
                                 #if NOTDEFINED
                                 #elsexxx
                                 #endif
                                 """;

            Assert.That(() => GetTokens<LexerErrorListener>(input, out _, []),
                Throws.Exception.With.InnerException.TypeOf<LexerNoViableAltException>());
        }
    }

    [TestFixture]
    public class HashImport : LexerTest
    {
        [Test]
        public void WhenSimpleImport_ReturnsCorrectTokens()
        {
            const string input = """
                                 #import "test.asm"
                                 """;

            var actual = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                HASHIMPORT, STRING,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }

        [Test]
        public void WhenMissingLastDoubleQuote_FinishesImportModeAtEOL()
        {
            const string input = """
                                 #import "
                                 #if
                                 """;
            
            var actual = GetAllTokens(input);
            
            var expected = GetTokenTypes(
                HASHIMPORT, WS, OPEN_STRING, EOL,
                HASHIF,
                KickAssemblerLexer.Eof
            );
            
            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenMissingLastDoubleQuoteAndNextLineIsHashImport_FinishesImportModeAtEOL()
        {
            const string input = """
                                 #import "
                                 #import "tubo.asm"
                                 """;
            
            var actual = GetAllTokens(input);
            
            var expected = GetTokenTypes(
                HASHIMPORT, WS, OPEN_STRING, EOL,
                HASHIMPORT, WS, STRING,
                KickAssemblerLexer.Eof
            );
            
            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
    }
    [TestFixture]
    public class HashImportIf : LexerTest
    {
        [Test]
        public void WhenConditionIsTrue_AddsReferencedFile()
        {
            const string input = """
                                 #importif DEFINED "test.asm"
                                 """;

            var actual = GetLexer<LexerErrorListener>(input, "DEFINED")
                .ReferencedFiles.Select(r => r.RelativeFilePath)
                .ToImmutableArray();

            ImmutableArray<string> expected = ["test.asm"];

            Assert.That(actual, Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenConditionIsFalse_DoesNotAddReferencedFile()
        {
            const string input = """
                                 #importif NOTDEFINED "test.asm"
                                 """;

            var actual = GetLexer<LexerErrorListener>(input)
                .ReferencedFiles.Select(r => r.RelativeFilePath)
                .ToImmutableArray();

            Assert.That(actual, Is.Empty);
        }
        [Test]
        public void FirstNextLineToken_ShouldNotBeHashImport()
        {
            const string input = """
                                 #importif DEFINED "test"
                                 #impo
                                 #define ONE
                                 """;
            
            var actual = GetAllTokens(input);

            var expected = GetTokenTypes(
                HASHIMPORTIF, IIF_CONDITION, STRING, EOL,
                HASH, UNQUOTED_STRING, EOL,
                HASHDEFINE, HD_WS,DEFINED_TOKEN,
                KickAssemblerLexer.Eof
            );
            
            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
    }

    [TestFixture]
    public class String : LexerTest
    {
        [Test]
        public void WhenOnlyNormalString_ReturnsString()
        {
            const string input = """
                                 "test.asm"
                                 """;

            var actual = GetAllTokens(input);

            var expected = GetTokenTypes(
                STRING,
                KickAssemblerLexer.Eof
            );
            
            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenStringWithNestedDoubleQuote_ReturnsStringAndRestAsUnquotedStringEndingWithDoubleQuote()
        {
            const string input = """
                                 ""test"
                                 """;

            var actual = GetAllTokens(input);

            var expected = GetTokenTypes(
                STRING, UNQUOTED_STRING, OPEN_STRING,
                KickAssemblerLexer.Eof
            );
            
            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenStringWithEscapedDoubleQuote_ReturnsString()
        {
            const string input = """
                                 "te\"st"
                                 """;

            var actual = GetAllTokens(input);

            var expected = GetTokenTypes(
                STRING,
                KickAssemblerLexer.Eof
            );
            
            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
    }
    
    [TestFixture]
    public class HashDefine : LexerTest
    {
        [Test]
        public void WhenDynamicDefineSet_ReferencedFileContainsIt()
        {
            const string input = """
                                 #define ONE
                                 #import "multi_import.asm"
                                 """;
            var actual = GetLexer<LexerErrorListener>(input)
                .ReferencedFiles
                .Single();

            ImmutableArray<string> expected = ["ONE"];
            
            Assert.That(actual.InDefines, Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleDefinition_ShouldNotEatSpace()
        {
            const string input = """
                                 #define ONE
                                 """;
            
            var actual = GetAllTokens(input);

            var expected = GetTokenTypes(
                HASHDEFINE, HD_WS,DEFINED_TOKEN,
                KickAssemblerLexer.Eof
            );
            
            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenTwoImportsOfSameFile_AndDefineSymbolsAreDifferent_ReferencedFileContainsDifferentDefines()
        {
            const string input = """
                                 #define ONE
                                 #import "multi_import.asm"
                                 #define TWO
                                 #import "multi_import.asm"
                                 """;
            var actual = GetLexer<LexerErrorListener>(input)
                .ReferencedFiles
                .Select(rf => rf.InDefines)
                .ToImmutableArray();

            ImmutableArray<string> expectedFirst = ["ONE"];
            ImmutableArray<string> expectedSecond = ["ONE", "TWO"];
            
            Assert.That(actual.Length, Is.EqualTo(2));
            Assert.That(actual[0], Is.EquivalentTo(expectedFirst));
            Assert.That(actual[1], Is.EquivalentTo(expectedSecond));
        }
    }

    [TestFixture]
    public class Scope : LexerTest
    {
        [Test]
        public void WhenEmptyDefinition_ShouldParseCorrectly()
        {
            const string input = """
                                 { }
                                 """;
            
            var actual = GetAllTokens(input);

            var expected = GetTokenTypes(
                OPEN_BRACE, WS, CLOSE_BRACE,
                KickAssemblerLexer.Eof
            );
            
            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
    }

    [TestFixture]
    public class LexerErrors : LexerTest
    {
        [Test]
        public void SimpleError_ShouldResultInSingleError()
        {
            const string input = """
                                 #ifx
                                 """;

            var actual = GetTokens<KickAssemblerLexerErrorListener>(input, out var errorListener);
            
            Assert.That(errorListener.Errors.Length, Is.EqualTo(1));
            var error = errorListener.Errors.Single();
            Assert.That((error.Line, error.CharPositionInLine), Is.EqualTo((1, 3)));
        }
    }

    [TestFixture]
    public class Macro : LexerTest
    {
        [Test]
        public void WhenSimpleOneLineMacro_ReturnsCorrectTokens()
        {
            const string input = """
                                 .macro Tubo(one) { }
                                 """;

            var actual = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                MACRO, UNQUOTED_STRING, OPEN_PARENS, UNQUOTED_STRING, CLOSE_PARENS, OPEN_BRACE, CLOSE_BRACE,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
    }

    [TestFixture]
    public class Print : LexerTest
    {
        // [Test]
        // public void WhenSimpleExtendText_ReturnsDotAndUnquotedString()
        // {
        //     const string input = """
        //                          .printno
        //                          """;
        //
        //     var actual = GetTokens<LexerErrorListener>(input, out _);
        //
        //     var expected = GetTokenTypes(
        //         DOT_UNQUOTED_STRING,
        //         KickAssemblerLexer.Eof
        //     );
        //
        //     Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        // }
        [Test]
        public void WhenSimplePrint_ReturnsCorrectTokens()
        {
            const string input = """
                                 .print 1
                                 """;

            var actual = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                PRINT, DEC_NUMBER,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
    }

    [TestFixture]
    public class File : LexerTest
    {
        [Test]
        public void WhenSimpleFile_ReturnsCorrectTokens()
        {
            const string input = """
                                 .file [mbfiles]
                                 """;

            var actual = GetTokens<LexerErrorListener>(input, out _);

            var expected = GetTokenTypes(
                FILE, OPEN_BRACKET, UNQUOTED_STRING, CLOSE_BRACKET,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
    }
}

public static class LexerTestExtensions
{
    public static ImmutableArray<int> GetTokenTypes(this ImmutableArray<IToken> tokens)
    {
        return [..tokens.Select(t => t.Type)];
    }
}