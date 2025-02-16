using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Test.Mocks;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Grammar;

[TestFixture]
public class ParserTest: ParserBootstrap<ParserTest>
{
    [TestFixture]
    public class PreprocessorIf : ParserTest
    {
        [Test]
        public void WhenHashIfElse_ParsesWithoutErrors()
        {
            const string input = """
                                 #if ONE
                                 lda #5
                                 #else
                                 lda #7
                                 #endif
                                 """;
            
            Assert.DoesNotThrow(() => Run(input, p => p.preprocessorIf(), out ErrorListener _));
        }
        [Test]
        public void WhenHashIfElifElse_AndElseIsValid_ParsesWithoutErrors()
        {
            const string input = """
                                 #if ONE
                                 lda #5
                                 #elif TWO
                                 lda #6
                                 #else
                                 lda #7
                                 #endif
                                 """;
            
            Assert.DoesNotThrow(() => Run(input, p => p.preprocessorIf(), out ErrorListener _));
        }
        [Test]
        public void WhenHashIfElifElse_AndIfIsValid_ParsesWithoutErrors()
        {
            const string input = """
                                 #if ONE
                                 lda #5
                                 #elif TWO
                                 lda #6
                                 #else
                                 lda #7
                                 #endif
                                 """;
            
            Assert.DoesNotThrow(() => Run(input, p => p.preprocessorIf(), out ErrorListener _), "ONE");
        }
        [Test]
        public void WhenEmptyHashIf_ParsesWithoutErrors()
        {
            const string input = """
                                 #if ONE
                                 #endif
                                 """;
            
            Assert.DoesNotThrow(() => Run(input, p => p.preprocessorIf(), out ErrorListener _));
        }
        [Test]
        public void WhenHashIf_ParsesWithoutErrors()
        {
            const string input = """
                                 #if ONE
                                 lda #5
                                 #endif
                                 """;
            
            Assert.DoesNotThrow(() => Run(input, p => p.preprocessorIf(), out ErrorListener _));
        }
    }
    [TestFixture]
    public class DecNumber: ParserTest
    {
        [TestCase("55")]
        [TestCase("100")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.decNumber(), out ErrorListener _));
        }

        [TestCase("a55")]
        [TestCase("100b")]
        [TestCase("a100b")]
        [TestCase("aa")]
        [TestCase("")]
        public void TestInvalid(string input)
        {
            Assert.Throws<Exception>(() => Run(input, p => p.decNumber(), out ErrorListener _));
        }
    }

    [TestFixture]
    public class LabelName : ParserTest
    {
        [TestCase("!")]
        [TestCase("!tubo")]
        [TestCase("tubo")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.labelName(), out ErrorListener _));
        }
    }

    [TestFixture]
    public class CpuDirectiveName : ParserTest
    {
        [TestCase(".cpu _65c02")]
        [TestCase(".cpu _6502NoIllegals")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.cpuDirective(), out ErrorListener _));
        }
    }

    [TestFixture]
    public class EncodingDirective : ParserTest
    {
        [TestCase(".encoding \"screencode_mixed\"")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.encodingDirective(), out ErrorListener _));
        }   
    }
    [TestFixture]
    public class Instruction : ParserTest
    {
        [TestCase("ldy #0")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.instruction(), out ErrorListener _));
        }   
    }

    [TestFixture]
    public class TestAllSamples : ParserTest
    {
        [TestCase("Sample1")]
        [TestCase("Sample2")]
        [TestCase("Sample3")]
        [TestCase("Sample4")]
        public void TestValid(string input)
        {
            var content = LoadKickAssSample($"{input}.asm");
            Assert.DoesNotThrow(() => Run(content, p => p.program(), out ErrorListener _));
        }
    }

    [TestFixture]
    public class Errors : ParserTest
    {
        private (KickAssemblerParserErrorListener ErrorListner, KickAssemblerParserListener Listener) RunProgram(string text,
            params string[] defineSymbols)
        {
            var listener = new KickAssemblerParserListener();
            Run<KickAssemblerParser.ProgramContext, KickAssemblerParserErrorListener>(
                listener, text, p => p.program(), out var errors);
            return (errors, listener);
        }

        [Test]
        public void WhenSimpleError_HasSingleMatchingError()
        {
            const string input = """
                                 invalid
                                 """;

            var actual = RunProgram(input);
            
            Assert.That(actual.Listener.SyntaxErrors.Length, Is.EqualTo(1));
            var error = actual.Listener.SyntaxErrors.Single();
            Assert.That((error.Line, error.CharPositionInLine), Is.EqualTo((1, 0)));
        }
        [Test]
        public void WhenAnotherSimpleError_HasSingleMatchingError()
        {
            const string input = """
                                 #invalid
                                 """;

            var actual = RunProgram(input);
            
            Assert.That(actual.ErrorListner.Errors.Length, Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Tests data collection such as labels
    /// </summary>
    public class DataParsing : ParserTest
    {
        [TestFixture]
        public class Label : DataParsing
        {
            [TestCase("!:", ExpectedResult = "")]
            [TestCase("!tubo:", ExpectedResult = "tubo")]
            [TestCase("tubo:", ExpectedResult = "tubo")]
            public string? GivenInput_ExtractsLabelName(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<RetroDbgDataProvider.Models.Parsing.Label>();

                return actual?.SingleOrDefault()?.Name;
            }
            [TestCase("!:", ExpectedResult = true)]
            [TestCase("!tubo:", ExpectedResult = true)]
            [TestCase("tubo:", ExpectedResult = false)]
            public bool? GivenInput_ExtractsIsMultiOccurence(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<RetroDbgDataProvider.Models.Parsing.Label>();

                return actual?.SingleOrDefault()?.IsMultiOccurrence;
            }
        }

        [TestFixture]
        public class Var : DataParsing
        {
            [TestCase(".var tubo=5", ExpectedResult = "tubo")]
            public string? GivenInput_ExtractsVarName(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<Variable>();
                
                return actual?.SingleOrDefault()?.Name;
            }
        }
        [TestFixture]
        public class Const : DataParsing
        {
            [TestCase(".const tubo=5", ExpectedResult = "tubo")]
            public string? GivenInput_ExtractsConstName(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<Constant>();
                
                return actual?.SingleOrDefault()?.Name;
            }
            [TestCase(".const tubo=5", ExpectedResult = "5")]
            [TestCase(".const tubo= \"pingo\"", ExpectedResult = "\"pingo\"")]
            public string? GivenInput_ExtractsAssignment(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<Constant>();
                
                return actual?.SingleOrDefault()?.Value;
            }
        }

        [TestFixture]
        public class EnumValues : DataParsing
        {
            [TestCase(".enum { one, two }", ExpectedResult = "one,two")]
            [TestCase(".enum { one=5, two=$8 }", ExpectedResult = "one,two")]
            public string? GivenInput_ExtractsEnumValueNames(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<RetroDbgDataProvider.Models.Parsing.EnumValues>();
                
                return string.Join(",", actual?.SingleOrDefault()?.Values.Select(v => v.Name) ?? []);
            }

            [Test]
            public void WhenNoEnumValues_DoesNotAddRecord()
            {
                var actual = Run(".enum {}", p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<RetroDbgDataProvider.Models.Parsing.EnumValues>();
                
                Assert.That(actual, Is.Empty); 
            }
        }

        [TestFixture]
        public class Macros : DataParsing
        {
            [TestCase(".macro Tubo(one) { }", ExpectedResult = "Tubo")]
            [TestCase(".macro Tubo() { }", ExpectedResult = "Tubo")]
            public string? GivenInput_ExtractsMacroName(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<Macro>();

                return actual?.SingleOrDefault()?.Name;
            }
            [TestCase(".macro Tubo() { }", ExpectedResult = "")]
            [TestCase(".macro Tubo(one) { }", ExpectedResult = "one")]
            [TestCase(".macro Tubo(one,two) { }", ExpectedResult = "one,two")]
            public string? GivenInput_ExtractsArgumentsCorrectly(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<Macro>();

                return string.Join(",", actual?.SingleOrDefault()?.Arguments ?? []);
            }
        }
        [TestFixture]
        public class Functions : DataParsing
        {
            [TestCase(".function Tubo(one) { }", ExpectedResult = "Tubo")]
            [TestCase(".function Tubo() { }", ExpectedResult = "Tubo")]
            public string? GivenInput_ExtractsMacroName(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<Function>();

                return actual?.SingleOrDefault()?.Name;
            }
            [TestCase(".function Tubo() { }", ExpectedResult = "")]
            [TestCase(".function Tubo(one) { }", ExpectedResult = "one")]
            [TestCase(".function Tubo(one,two) { }", ExpectedResult = "one,two")]
            public string? GivenInput_ExtractsArgumentsCorrectly(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<Function>();

                return string.Join(",", actual?.SingleOrDefault()?.Arguments ?? []);
            }
        }

        [TestFixture]
        public class ForVariables : DataParsing
        {
            [TestCase(".for (var x=0;;) { }", ExpectedResult = "x")]
            public string? GivenInput_ExtractsVariableName(string input)
            {
                var actual = Run(input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<For>().SingleOrDefault()?.Variables;

                return actual?.SingleOrDefault()?.Name;
            }

            public record ExtractVariableRangeTestItem(string Input, (int Line, int Column) ExpectedStart, (int Line, int Column) ExpectedEnd);

            public static IEnumerable<ExtractVariableRangeTestItem> GetExtractsVariableRangeItems()
            {
                yield return new(".for (var x=0;;) { }",
                    new(0, 0), new(0, 20));
            }

            [TestCaseSource(nameof(GetExtractsVariableRangeItems))]
            public void GivenInput_ExtractsVariableStartPosition(ExtractVariableRangeTestItem td)
            {
                var actual = Run(td.Input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<For>().SingleOrDefault();

                var pos = actual!.Range!.Value.Start!;
                var actualPosition = (pos.Value.Line, pos.Value.Column);
                
                Assert.That(actualPosition, Is.EqualTo(td.ExpectedStart));
            }
            [TestCaseSource(nameof(GetExtractsVariableRangeItems))]
            public void GivenInput_ExtractsVariableEndPosition(ExtractVariableRangeTestItem td)
            {
                var actual = Run(td.Input, p => p.program(), out ErrorListener _)
                    .DefaultScope?.Elements.OfType<For>().SingleOrDefault();

                var pos = actual!.Range!.Value.End!;
                var actualPosition = (pos.Value.Line, pos.Value.Column);
                
                Assert.That(actualPosition, Is.EqualTo(td.ExpectedEnd));
            }
        }
    }
}