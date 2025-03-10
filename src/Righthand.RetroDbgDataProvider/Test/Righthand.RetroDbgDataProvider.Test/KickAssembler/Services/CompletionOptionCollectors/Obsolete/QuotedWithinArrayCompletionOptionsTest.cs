﻿// using NUnit.Framework;
// using Righthand.RetroDbgDataProvider.KickAssembler.Models;
// using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
//
// namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;
//
// public class QuotedWithinArrayCompletionOptionsTest
// {
//     [TestFixture]
//     public class GetArrayValues : QuotedWithinArrayCompletionOptionsTest
//     {
//     
// [TestFixture]
//     public class IsCursorWithinArray : QuotedWithinArrayCompletionOptionsTest
//     {
//         [TestCase(".file [name=\"")]
//         [TestCase(".file [segments=\"Code\",name=\"")]
//         [TestCase(".file [segments = \"Code\" , name = \"")]
//         [TestCase(".segment Base [prgFiles = \"")]
//         [TestCase(".segment Base [prgFiles=\"test.prg,")]
//         [TestCase("zpCode: .segment Base [prgFiles=\"test.prg,")]
//         [TestCase("\"\".file [name=\"")]
//         [TestCase("\"//\".file [name=\"")]
//         public void GivenSampleInputThatPutsCursorWithinArray_ReturnsNonNullResult(string line)
//         {
//             var actual =
//                 QuotedWithinArrayCompletionOptions.IsCursorWithinArray(line, 0, line.Length, line.Length-1,
//                     KickAssemblerParsedSourceFile.ValuesCount.Multiple);
//
//             Assert.That(actual, Is.Not.Null);
//         }
//
//         [TestCase(".file [name=\"\"")]
//         [TestCase(".file [segments=\"Code\" name=\"")]
//         [TestCase(".file segments = \"Code\" , name = \"")]
//         [TestCase("zpCode: .file segments = \"Code\" , name = \"")]
//         public void GivenSampleInputThatDoesNotPutCursorWithinArray_ReturnsNullResult(string line)
//         {
//             var actual =
//                 QuotedWithinArrayCompletionOptions.IsCursorWithinArray(line, 0, line.Length, line.Length-1,
//                     KickAssemblerParsedSourceFile.ValuesCount.Multiple);
//
//             Assert.That(actual, Is.Null);
//         }
//
//         [TestCase(".file [name=\"", ExpectedResult = 6)]
//         [TestCase(".file [segments=\"Code\",name=\"", ExpectedResult = 6)]
//         [TestCase(".file  [ segments = \"Code\" , name = \"", ExpectedResult = 7)]
//         [TestCase("zpCode: .file  [ segments = \"Code\" , name = \"", ExpectedResult = 15)]
//         public int? GivenSampleInput_ReturnsOpenBracketColumnIndex(string line)
//         {
//             return QuotedWithinArrayCompletionOptions
//                 .IsCursorWithinArray(line, 0, line.Length, line.Length-1,
//                     KickAssemblerParsedSourceFile.ValuesCount.Multiple)?.OpenBracketColumn;
//         }
//
//         [Test]
//         public void WhenNotSupportingMultipleValues_AndCommaDelimitedValueIsInFront_ReturnsNull()
//         {
//             string line = ".file [name=\"value,";
//             var actual =
//                 QuotedWithinArrayCompletionOptions.IsCursorWithinArray(line, 0, line.Length, line.Length-1,
//                     KickAssemblerParsedSourceFile.ValuesCount.Single);
//
//             Assert.That(actual, Is.Null);
//         }
//     }
//
//     [TestFixture]
//     public class FindFirstArrayDelimiterPosition : QuotedWithinArrayCompletionOptionsTest
//     {
//         [TestCase("", 0, ExpectedResult = null)]
//         [TestCase("tubo,", 0, ExpectedResult = 4)]
//         [TestCase("tubo,", 2, ExpectedResult = 4)]
//         [TestCase("tubo , ", 2, ExpectedResult = 5)]
//         [TestCase("tubo , \"", 2, ExpectedResult = 5)]
//         [TestCase("tubo \" ,", 2, ExpectedResult = 5)]
//         [TestCase("tubo", 2, ExpectedResult = null)]
//         public int? GivenSampleInput_ReturnsCorrectResult(string line, int cursor)
//         {
//             return QuotedWithinArrayCompletionOptions.FindFirstArrayDelimiterPosition(line, cursor);
//         }
//     }
// }