using System.Collections.Frozen;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class DirectivePropertiesTest
{
    [TestFixture]
    public class GetValueTypes : DirectivePropertiesTest
    {
        [Test]
        public void WhenSelectingDirectiveWithType_WithoutType_ReturnsAllValues()
        {
            var actual = DirectiveProperties.GetValueTypes(".import", null)!
                .OfType<FileDirectiveValueType>()
                .Select(fd => fd.FileExtension)
                .ToFrozenSet();

            FrozenSet<string> expected = [".bin", ".txt", ".c64", ".asm"];
            Assert.That(actual, Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSelectingDirectiveWithType_WithType_ReturnsTypeValue()
        {
            var actual = DirectiveProperties.GetValueTypes(".import", "c64")!
                .OfType<FileDirectiveValueType>()
                .Select(fd => fd.FileExtension)
                .ToFrozenSet();

            FrozenSet<string> expected = [".c64"];
            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}