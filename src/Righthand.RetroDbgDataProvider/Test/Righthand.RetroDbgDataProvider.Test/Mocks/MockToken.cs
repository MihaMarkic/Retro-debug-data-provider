using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Test.Mocks;

[DebuggerDisplay("{TypeText,q}")]
public class MockToken: IToken
{
    public string Text { get; init; } = "";
    public int Type { get; init; } = -1;
    public int Line { get; init; } = -1;
    public int Column { get; init; } = -1;
    public int Channel { get; init; } = -1;
    public int TokenIndex { get; init; } = -1;
    public int StartIndex { get; init; } = -1;
    public int StopIndex { get; init; } = -1;
    public ITokenSource TokenSource { get; init; } = null!;
    public ICharStream InputStream { get; init; } = null!;
    public string TypeText => KickAssemblerLexer.DefaultVocabulary.GetSymbolicName(Type);
}