using System.Collections.Frozen;
using System.Diagnostics;
using Righthand.RetroDbgDataProvider.KickAssembler.PreprocessorCondition;
using Righthand.RetroDbgDataProvider.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler;

partial class KickAssemblerLexer
{
    public HashSet<string> DefinedSymbols { get; init; } = new();   
    private int? ModeOnDefaultEol { get; set; }
    public bool IsImportOnce { get; private set; }
    public List<ReferencedFileInfo> ReferencedFiles { get; } = new();
    private bool IsDefined(string text) => PreprocessorConditionEvaluator.IsDefined(DefinedSymbols.ToFrozenSet(), text);

    public override void PushMode(int m)
    {
        Debug.WriteLine($"Push mode {modeNames[m]}");
        base.PushMode(m);
    }
    private void AddReferencedFileInfo(int tokenStartLine, int tokenStartColumn, string text)
    {
        var info = new ReferencedFileInfo(tokenStartLine, tokenStartColumn, text.Trim('"'),
            DefinedSymbols.ToFrozenSet());
        ReferencedFiles.Add(info);
    }

    public override int PopMode()
    {
        int oldMode = base.PopMode();
        Debug.WriteLine($"Push mode {modeNames[oldMode]}");
        return oldMode;
    }
}