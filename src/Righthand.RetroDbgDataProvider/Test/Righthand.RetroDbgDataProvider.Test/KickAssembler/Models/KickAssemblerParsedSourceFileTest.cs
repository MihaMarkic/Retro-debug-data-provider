using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using AutoFixture;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using CommonTokenStream = Antlr4.Runtime.CommonTokenStream;
using Label = Righthand.RetroDbgDataProvider.Models.Parsing.Label;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Models;

public class KickAssemblerParsedSourceFileTest : BaseTest<KickAssemblerParsedSourceFile>
{
    readonly DateTimeOffset _lastModified = new DateTimeOffset(2020, 09, 09, 09, 01, 05, TimeSpan.Zero);

    static (KickAssemblerLexer Lexer, CommonTokenStream TokenStream, KickAssemblerParser Parser,
        KickAssemblerParserListener ParserListener,
        KickAssemblerLexerErrorListener LexerErrorListener, KickAssemblerParserErrorListener ParserErrorListener,
        ImmutableArray<IToken> AllTokens)
        GetParsed(string text, params string[] definitions)
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input)
        {
            DefinedSymbols = definitions.ToHashSet(),
        };
        var lexerErrorListener = new KickAssemblerLexerErrorListener();
        var stream = new CommonTokenStream(lexer);
        var parserErrorListener = new KickAssemblerParserErrorListener();
        var parserListener = new KickAssemblerParserListener();
        var parser = new KickAssemblerParser(stream) { BuildParseTree = true };
        parser.AddParseListener(parserListener);
        parser.AddErrorListener(parserErrorListener);
        stream.Fill();
        ImmutableArray<IToken> allTokens = [..stream.GetTokens()];
        return (lexer, stream, parser, parserListener, lexerErrorListener, parserErrorListener, allTokens);
    }

    static (ImmutableArray<IToken> Tokens, FrozenDictionary<int, ImmutableArray<IToken>> AllTokensByLineMap) GetTokens(string text, params string[] definitions)
    {
        var (_, tokenStream, _, _, _, _, _) = GetParsed(text.Replace("|", ""), definitions);
        ImmutableArray<IToken> tokens = [..tokenStream.GetTokens().Where(t => t.Channel == 0)];
        var allTokensByLineMap = tokens
            .GroupBy(t => t.Line - 1)
            .ToFrozenDictionary(g => g.Key, g => g.OrderBy(t => t.Column).ToImmutableArray());
        return (tokens, allTokensByLineMap);
    }

    [TestFixture]
    public class GetIgnoredDefineContent : KickAssemblerParsedSourceFileTest
    {
        [Test]
        public void WhenEmptySource_ReturnsEmptyArray()
        {
            var input = GetParsed("");
            var target = new KickAssemblerParsedSourceFile("fileName", "", [],
                FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
                FrozenSet<string>.Empty, FrozenSet<string>.Empty, FrozenSet<SegmentDefinitionInfo>.Empty,
                ImmutableList<Label>.Empty, ImmutableList<string>.Empty, ImmutableList<Constant>.Empty, 
                _lastModified, liveContent: null, isImportOnce: false,
                input.LexerErrorListener.Errors, input.ParserErrorListener.Errors);

            var actual = target.GetIgnoredDefineContent(CancellationToken.None);

            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void WhenSourceWithUndefinedContent_ReturnsArrayContainingSingleRange()
        {
            var input = GetParsed("""
                                  #if UNDEFINED
                                    bla bla
                                  #endif
                                  """.FixLineEndings());
            var target = new KickAssemblerParsedSourceFile("fileName", "", input.AllTokens,
                FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
                FrozenSet<string>.Empty, FrozenSet<string>.Empty, FrozenSet<SegmentDefinitionInfo>.Empty,
                ImmutableList<Label>.Empty, ImmutableList<string>.Empty, ImmutableList<Constant>.Empty, 
                _lastModified, liveContent: null, isImportOnce: false,
                input.LexerErrorListener.Errors, input.ParserErrorListener.Errors);

            var actual = target.GetIgnoredDefineContent(CancellationToken.None);

            ImmutableArray<MultiLineTextRange> expected = [new(new TextCursor(2, 0), new TextCursor(2, 10))];
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void WhenSourceWithTwoUndefinedContents_ReturnsArrayContainingBothRanges()
        {
            var input = GetParsed("""
                                  #if UNDEFINED
                                    bla bla
                                  #elif UNDEFINED
                                    yada yada
                                  #endif
                                  """.FixLineEndings());
            var target = new KickAssemblerParsedSourceFile("fileName", "", input.AllTokens,
                FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
                FrozenSet<string>.Empty, FrozenSet<string>.Empty, FrozenSet<SegmentDefinitionInfo>.Empty,
                ImmutableList<Label>.Empty, ImmutableList<string>.Empty, ImmutableList<Constant>.Empty, 
                _lastModified, liveContent: null, isImportOnce: false,
                input.LexerErrorListener.Errors, input.ParserErrorListener.Errors);

            var actual = target.GetIgnoredDefineContent(CancellationToken.None);

            ImmutableArray<MultiLineTextRange> expected =
            [
                new(new TextCursor(2, 0), new TextCursor(2, 10)),
                new(new TextCursor(4, 0), new TextCursor(4, 12))
            ];
            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }

    [TestFixture]
    public class UpdateLexerAnalysisWithParserAnalysis : KickAssemblerParsedSourceFileTest
    {
        [Test]
        public void WhenNoFileReferencesFromParser_LexerResultsAreReturned()
        {
            var firstToken = Fixture.Create<IToken>();
            var secondToken = Fixture.Create<IToken>();

            ImmutableArray<KickAssemblerParsedSourceFile.LexerBasedSyntaxResult> source =
            [
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, firstToken,
                    new SyntaxItem(0, 0, TokenType.Number)),
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, secondToken,
                    new SyntaxItem(0, 0, TokenType.String))
            ];

            var actual =
                KickAssemblerParsedSourceFile.UpdateLexerAnalysisWithParserAnalysis(source,
                    FrozenDictionary<IToken, ReferencedFileInfo>.Empty);

            Assert.That(actual, Is.EquivalentTo(source));
        }

        [Test]
        public void WhenFileReferencesFromParserMatchesSecondToken_SyntaxItemIsReplaced()
        {
            var firstToken = Fixture.Create<IToken>();
            var secondToken = Fixture.Create<IToken>();

            ImmutableArray<KickAssemblerParsedSourceFile.LexerBasedSyntaxResult> source =
            [
                new(0, firstToken, new SyntaxItem(0, 0, TokenType.Number)),
                new(0, secondToken, new SyntaxItem(0, 0, TokenType.String))
            ];

            var fileReferences = new Dictionary<IToken, ReferencedFileInfo>
            {
                { secondToken, new ReferencedFileInfo(0, 0, "file.asm","file.asm", FrozenSet<string>.Empty) }
            }.ToFrozenDictionary();

            var actual =
                KickAssemblerParsedSourceFile.UpdateLexerAnalysisWithParserAnalysis(source, fileReferences)
                    .ToImmutableArray();

            Assert.That(actual[0], Is.EqualTo(source[0]));
            var item = (FileReferenceSyntaxItem)actual[1].Item;
            Assert.That(item, Is.Not.EqualTo(source[1].Item));
            Assert.That(item.ReferencedFile.RelativeFilePath, Is.EqualTo("file.asm"));
        }
    }

    [TestFixture]
    public class GetCompletionOption : KickAssemblerParsedSourceFileTest
    {
        private static CompletionOptionContext NoOpContext { get; } = new (
            Substitute.For<IProjectServices>()
        );

        private static CompletionOptionContext CreateContext(ImmutableArray<string> segments, FrozenSet<string> preprocessorSymbols)
        {
            var files = new Dictionary<string, FrozenSet<string>>
            {
                {
                    "Project",
                    [
                        "one.prg", "two.prg", "sub1/one.prg".ToPath(), "sub2/two.prg".ToPath(),
                        "sidOne.sid", 
                        "tubo.bin", "tubo2.bin",
                        "alfa.c64", "beta.c64",
                        "bingo.txt", "beta.txt", "delta.txt"
                    ]                
                }
            }.ToFrozenDictionary();
            var directories = new Dictionary<string, FrozenSet<string>>
            {
                {
                    "Project",
                    [
                        "sub1", "sub2", "anotherSub", "sub1/nested".ToPath()
                    ]                
                }
            }.ToFrozenDictionary();
            var projectServices = Substitute.For<IProjectServices>();
            projectServices.CollectSegments().Returns(segments);
            projectServices.CollectPreprocessorSymbols().Returns(preprocessorSymbols);
            projectServices.GetMatchingFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<FrozenSet<string>>(), Arg.Any<ICollection<string>>())
                .Returns(a =>
                {
                    var source = files["Project"];
                    var fileSet = new HashSet<string>();
                    var filter = a.ArgAt<string>(1);
                    string filterDirectory = Path.GetDirectoryName(filter) ?? "";
                    foreach (var f in source)
                    {
                        var extensions =  a.ArgAt<FrozenSet<string>>(2);
                        string fileDirectory = Path.GetDirectoryName(f) ?? "";
                        if (fileDirectory.Equals(filterDirectory))
                        {
                            var excluded = a.ArgAt<ICollection<string>>(2).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
                            if (f.StartsWith(filter, StringComparison.Ordinal))
                            {
                                var fileExtension = Path.GetExtension(f);
                                if (extensions.Contains("*") || extensions.Contains(fileExtension))
                                {
                                    if (!excluded.Contains(f))
                                    {
                                        fileSet.Add(f);
                                    }
                                }
                            }
                        }
                    }
            
                    return new Dictionary<ProjectFileKey, FrozenSet<string>> { { new(ProjectFileOrigin.Project, ""), fileSet.ToFrozenSet(StringComparer.OrdinalIgnoreCase) } }
                        .ToFrozenDictionary();
                });
            projectServices.GetMatchingDirectories(Arg.Any<string>(), Arg.Any<string>())
                .Returns(a =>
                {
                    var source = directories["Project"];
                    var filter = a.ArgAt<string>(1)!;
                    var startDirectory = Path.GetDirectoryName(filter) ?? "";
                    var rootDirectoryName = Path.GetFileName(filter);
                    var fileSet = source
                        .Where(d => 
                            Path.GetFileName(d)!.StartsWith(rootDirectoryName, StringComparison.Ordinal)
                            && Path.GetDirectoryName(d)!.Equals(startDirectory, StringComparison.Ordinal))
                        .Distinct()
                        .ToFrozenSet();
                    return new Dictionary<ProjectFileKey, FrozenSet<string>> { { new(ProjectFileOrigin.Project, ""), fileSet.ToFrozenSet(StringComparer.OrdinalIgnoreCase) } }
                        .ToFrozenDictionary();
                });
            return new(projectServices);
        }

        /// <summary>
        /// First column is -1
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static (int Column, int LineNumber, int Start, int End, string Text, int TextLength) GetPosition(string text)
        {
            var lines = text.Split(Environment.NewLine);
            int index = 0;
            for (int i = 0; i < lines.Length; ++i)
            {
                string lineText = lines[i];
                int caretIndex = lineText.IndexOf('|');
                if (caretIndex >= 0)
                {
                    // 1 should be subtracted from line length because of caret
                    return (index + caretIndex, i, index, index + lineText.Length - 1, text.Replace("|", ""), lineText.Length - 1);
                }

                if (index > 0)
                {
                    index += Environment.NewLine.Length;
                }

                index += lineText.Length;
            }

            throw new Exception("Couldn't find caret");
        }

        private static CompletionOption? RunTest(string text, TextChangeTrigger trigger)
        {
            var (tokens, tokensByLine) = GetTokens(text);
            var (column, lineNumber, lineTextStart, lineTextEnd, realText, textLength) = GetPosition(text);
            var context = CreateContext(["one1", "one2", "two1"], ["ALFA", "BETA", "BFTA"]);
            var actual = KickAssemblerParsedSourceFile.GetCompletionOption(tokens, tokensByLine, trigger, TriggerChar.DoubleQuote, lineNumber, column, realText, lineTextStart,
                textLength, "", context);
            return actual;
        }

        [TestFixture]
        public class ArrayPropertyNames : GetCompletionOption
        {
            [TestCase(".file [|", "mbfiles,name,type,segments")]
            [TestCase(".file [mbfiles|", "")]
            [TestCase(".file [m|", "mbfiles")]
            [TestCase(".file [mbfiles,|", "name,type,segments")]
            [TestCase(".file [mbfiles,type, name,|", "segments")]
            public void GivenTestCaseForCharacterTypedTrigger_ReturnsSuggestedTexts(string text, string? expectedText)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                var actual = actualOption?.Suggestions.Select(s => s.Text).ToImmutableArray();
                var expected = expectedText?.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToImmutableArray();

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        [TestFixture]
        public class SegmentArrayPropertyValues : GetCompletionOption
        {

            [TestCase(".segmentdef Base [segments=\"|\"]", "one1,one2,two1")]
            [TestCase(".segmentdef Base [segments=|\"", "")]
            [TestCase(".segmentdef Base [segments=\"o|\"]", "one1,one2")]
            [TestCase(".segmentdef Base [segments=\"t,o|\"]", "one1,one2")]
            [TestCase(".segmentdef Base [segments=\"t\"|", null)]
            [TestCase(".segmentdef Base [segments=\"|]", "one1,one2,two1")]
            public void GivenTestCaseForCharacterTypedTrigger_ReturnsSuggestedTexts(string text, string? expectedText)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                var actual = actualOption?.Suggestions.Select(s => s.Text).ToImmutableArray();
                var expected = expectedText?.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToImmutableArray();

                Assert.That(actual, Is.EqualTo(expected));
            }

            [TestCase(".segmentdef Base [segments=\"|\"]", ExpectedResult = "")]
            [TestCase(".segmentdef Base [segments=|\"", ExpectedResult = "")]
            [TestCase(".segmentdef Base [segments=\"o|\"]", ExpectedResult = "o")]
            [TestCase(".segmentdef Base [segments=\"t,o|\"]", ExpectedResult = "o")]
            [TestCase(".segmentdef Base [segments=\"|]", ExpectedResult = "")]
            public string? GivenTestCaseForCharacterTypedTrigger_ReturnsValueRoot(string text)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                return actualOption?.RootText;
            }

            [TestCase(".segmentdef Base [segments=|", ExpectedResult = "\"")]
            [TestCase(".segmentdef Base [segments=|\"", ExpectedResult = "")]
            [TestCase(".segmentdef Base [segments=\"o|\"]", ExpectedResult = "")]
            [TestCase(".segmentdef Base [segments=\"t,o|\"]", ExpectedResult = "")]
            public string? GivenTestCaseForCharacterTypedTrigger_PrependsDoubleQuotesWhenExpected(string text)
            {
                var actual = RunTest(text, TextChangeTrigger.CharacterTyped);

                return actual?.PrependText;
            }

            [TestCase(".segmentdef Base [segments=|", ExpectedResult = "")]
            [TestCase(".segmentdef Base [segments=|\"", ExpectedResult = "")]
            [TestCase(".segmentdef Base [segments=\"o|\"]", ExpectedResult = "")]
            [TestCase(".segmentdef Base [segments=\"t,o|\"]", ExpectedResult = "")]
            [TestCase(".segmentdef Base [segments=\"|]", ExpectedResult = "")]
            public string? GivenTestCaseForCharacterTypedTrigger_AppendsDoubleQuotesWhenExpected(string text)
            {
                var actual = RunTest(text, TextChangeTrigger.CharacterTyped);

                return actual?.AppendText;
            }

            [TestCase(".segmentdef Base [segments=\"o|\"]", ExpectedResult = 1)]
            [TestCase(".segmentdef Base [segments=\"|o\"]", ExpectedResult = 1)]
            [TestCase(".segmentdef Base [segments=|\"o\"]", ExpectedResult = 1)]
            [TestCase(".segmentdef Base [segments=\"o|]", ExpectedResult = 1)]
            [TestCase(".segmentdef Base [segments=\"|o]", ExpectedResult = 1)]
            [TestCase(".segmentdef Base [segments=\"|", ExpectedResult = 0)]
            [TestCase(".segmentdef Base [segments=\"t,o|\"]", ExpectedResult = 1)]
            [TestCase(".segmentdef Base [segments=\"t,o|x\"]", ExpectedResult = 2)]
            [TestCase(".segmentdef Base [segments=\"t,o|x,\"]", ExpectedResult = 2)]
            [TestCase(".segmentdef Base [segments=\"t,o|x\"", ExpectedResult = 2)]
            [TestCase(".segmentdef Base [segments=\"t,o|x,\"", ExpectedResult = 2)]
            [TestCase(".segmentdef Base [segments=\"t,o|x", ExpectedResult = 2)]
            [TestCase(".segmentdef Base [segments=\"t,o|x,", ExpectedResult = 2)]
            [TestCase(".segmentdef Base [segments=\"|]", ExpectedResult = 0)]
            public int? GivenTestCaseForCharacterTypedTrigger_ReplacementLengthIsCorrect(string text)
            {
                var actual = RunTest(text, TextChangeTrigger.CharacterTyped);

                return actual?.ReplacementLength;
            }
        }
        [TestFixture]
        public class FileArrayPropertyValues : GetCompletionOption
        {

            [TestCase(".file [name=|", "one.prg,two.prg")]
            [TestCase(".file [name=\"o|", "one.prg")]
            [TestCase(".file [name=\"one|", "one.prg")]
            [TestCase(".file [name=\"one.|", "one.prg")]
            [TestCase(".file [name=\"one.p|", "one.prg")]
            [TestCase(".file [name=\"one.x|", "")]
            public void GivenTestCaseForCharacterTypedTrigger_ReturnsSuggestedFileTexts(string text, string? expectedText)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                var actual = actualOption?.Suggestions.OfType<FileSuggestion>().Select(s => s.Text).ToImmutableArray();
                var expected = expectedText!.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToImmutableArray();

                Assert.That(actual, Is.EquivalentTo(expected));
            }

            [TestCase(".file [name=\"one.x|", "")]
            [TestCase(".file [name=\"s|", "sub1,sub2")]
            [TestCase(".file [name=\"sub|", "sub1,sub2")]
            public void GivenTestCaseForCharacterTypedTrigger_ReturnsSuggestedDirectoryTexts(string text, string? expectedText)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                var actual = actualOption?.Suggestions.OfType<DirectorySuggestion>().Select(s => s.Text).ToImmutableArray();
                var expected = expectedText?.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToImmutableArray();

                Assert.That(actual, Is.EqualTo(expected));
            }

            [TestCase(".file [name=|", ExpectedResult = "\"")]
            [TestCase(".file [name=\"|", ExpectedResult = "")]
            public string? GivenTestCaseForCharacterTypedTrigger_PrependsDoubleQuotesWhenExpected(string text)
            {
                var actual = RunTest(text, TextChangeTrigger.CharacterTyped);

                return actual?.PrependText;
            }
            [TestCase(".file [name=|", ExpectedResult = "\"")]
            [TestCase(".file [name=|\"", ExpectedResult = "")]
            public string? GivenTestCaseForCharacterTypedTrigger_AppendsDoubleQuotesWhenExpected(string text)
            {
                var actual = RunTest(text, TextChangeTrigger.CharacterTyped);

                return actual?.AppendText;
            }
        }

        [TestFixture]
        public class FileNamesInDirective : GetCompletionOption
        {
            [TestCase(".import \"|", "tubo.bin,tubo2.bin,alfa.c64,beta.c64,bingo.txt,beta.txt,delta.txt")]
            [TestCase(".import c64 \"|", "alfa.c64,beta.c64")]
            //[TestCase(".import c64 |", "alfa.c64,beta.c64")] // not supporting typing without double quotes
            [TestCase(".import c64 \"a|", "alfa.c64")]
            public void GivenTestCaseForCharacterTypedTrigger_ReturnsSuggestedFileTexts(string text, string? expectedText)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                var actual = actualOption!.Value.Suggestions.OfType<FileSuggestion>().Select(s => s.Text).ToFrozenSet();
                var expected = expectedText!.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToFrozenSet();

                Assert.That(actual, Is.EquivalentTo(expected));
            }
            [TestCase(".import c64x \"|")]
            [TestCase(".importx c64 \"|")]
            [TestCase(".importx \"|")]
            public void GivenInvalidTestCaseForCharacterTypedTrigger_ReturnsNull(string text)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                Assert.That(actualOption, Is.Null);
            }
        }

        [TestFixture]
        public class EnumerableValuesInDirective : GetCompletionOption
        {
            [TestCase(".encoding \"|", "ascii,petscii_mixed,petscii_upper,screencode_mixed,screencode_upper")]
            [TestCase(".encoding \"a|", "ascii")]
            [TestCase(".encoding \"screencode_|", "screencode_mixed,screencode_upper")]
            public void GivenTestCaseForCharacterTypedTrigger_ReturnsSuggestedEnumerableValues(string text, string? expectedText)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                var actual = actualOption?.Suggestions.Select(s => s.Text).ToFrozenSet();
                var expected = expectedText!.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToFrozenSet();

                Assert.That(actual, Is.EquivalentTo(expected));
            }
        }

        [TestFixture]
        public class DirectiveNameCompletion : GetCompletionOption
        {
            [TestCase(".enc|", ".encoding")]
            public void GivenTestCaseForCharacterTypedTrigger_ReturnsSuggestedDirectiveNames(string text, string? expectedText)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                var actual = actualOption?.Suggestions.Select(s => s.Text).ToFrozenSet();
                var expected = expectedText!.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToFrozenSet();

                Assert.That(actual, Is.EquivalentTo(expected));
            }
        }
        [TestFixture]
        public class DirectiveOptions : GetCompletionOption
        {
            [TestCase(".import c| \"alfa.c64", "c64")]
            public void GivenTest_ReturnsSuggestedFileTexts(string text, string? expectedText)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                var actual = actualOption!.Value.Suggestions.Select(s => s.Text).ToFrozenSet();
                var expected = expectedText!.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToFrozenSet();

                Assert.That(actual, Is.EquivalentTo(expected));
            }
        }

        [TestFixture]
        public class PreprocessorIfExpressions : GetCompletionOption
        {
            [TestCase("#if A|", "ALFA")]
            [TestCase("#if B|", "BETA,BFTA")]
            [TestCase("#if BE|", "BETA")]
            [TestCase("#if A+B|", "BETA,BFTA")]
            [TestCase("#if A+|", "ALFA,BETA,BFTA")]
            [TestCase("#if ALFA|", "")]
            public void GivenTestCase_ReturnsSuggestedSymbolNames(string text, string? expectedText)
            {
                var actualOption = RunTest(text, TextChangeTrigger.CharacterTyped);

                var actual = actualOption!.Value.Suggestions.Select(s => s.Text).ToFrozenSet();
                var expected = expectedText!.Split(',').Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToFrozenSet();
                
                Assert.That(actual, Is.EquivalentTo(expected));
            }
        }
    }
    [TestFixture]
    public class GetTokenIndexAtLocation : KickAssemblerParsedSourceFileTest
    {
        [Test]
        public void WhenNoSourceCode_ReturnsZero()
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 0);

            Assert.That(actual, Is.Zero);
        }

        [Test]
        public void WhenLineIsLessThanZero_ReturnsNull()
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, -1, 0);

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void WhenOffsetIsOutOfBounds_ReturnsNull()
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 99, 0);

            Assert.That(actual, Is.Null);
        }

        [TestCase(" ")]
        [TestCase("\t")]
        public void WhenHiddenChar_ReturnsOne(string input)
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap(input);

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 1);

            Assert.That(actual, Is.EqualTo(1));
        }

        [Test]
        public void WhenWithinDefaultToken_ReturnsItsIndex()
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("#import \"file.asm\"");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 11 - 1);

            Assert.That(actual, Is.EqualTo(2));
        }

        [Test]
        public void WhenRightAfterDoubleQuote_ReturnsIndexOfDoubleQuote()
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("#import \"");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 9 - 1);

            Assert.That(actual, Is.EqualTo(2));
        }
    }

    [TestFixture]
    public class TestTrackImport : KickAssemblerParsedSourceFileTest
    {
        private KickAssemblerDbgParser _parser = default!;
        private KickAssemblerProgramInfoBuilder _infoBuilder = default!;
        private DbgData _dbg = default!;

        [SetUp]
        public void Setup()
        {
            _parser = new KickAssemblerDbgParser(Substitute.For<ILogger<KickAssemblerDbgParser>>());
            _infoBuilder =
                new KickAssemblerProgramInfoBuilder(Substitute.For<ILogger<KickAssemblerProgramInfoBuilder>>());
        }

        [Test]
        public async Task Test()
        {
            var sample = LoadKickAssSampleFile("SimpleImport", "main.dbg");
            _dbg = await _parser.LoadContentAsync(sample, "path");
            var assemblyInfo = await _infoBuilder.BuildAppInfoAsync("path", _dbg);
        }
    }

    [TestFixture]
    public class GetMatches : KickAssemblerParsedSourceFileTest
    {
        [Test]
        public void WhenEmptyList_FindsNoMatches()
        {
            var actual = KickAssemblerParsedSourceFile.GetMatches("ab", 1, ReadOnlySpan<string>.Empty);

            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void WhenValidRoot_FindsAllMatches()
        {
            ImmutableArray<string> list = ["#a", "#abb", "#abc", "#d"];

            var actual = KickAssemblerParsedSourceFile.GetMatches("ab", 1, list.AsSpan());

            ImmutableArray<string> expected = ["#abb", "#abc"];
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void WhenRootHasNoMatched_ReturnsEmptyList()
        {
            ImmutableArray<string> list = ["#a", "#abb", "#abc", "#d"];

            var actual = KickAssemblerParsedSourceFile.GetMatches("x", 1, list.AsSpan());

            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void WhenRootDoesNotStartAfterHash_ReturnsEmptyList()
        {
            ImmutableArray<string> list = ["#a", "#abb", "#abc", "#d"];

            var actual = KickAssemblerParsedSourceFile.GetMatches("b", 1, list.AsSpan());

            Assert.That(actual, Is.Empty);
        }
    }
}