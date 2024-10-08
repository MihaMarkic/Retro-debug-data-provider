using System.Diagnostics;

namespace Righthand.RetroDbgDataProvider.KickAssembler;

partial class KickAssemblerLexer
{
    public HashSet<string> DefinedSymbols { get; init; } = new();
    private int? ModeOnDefaultEol { get; set; }

    public override void PushMode(int m)
    {
        Debug.WriteLine($"Push mode {modeNames[m]}");
        base.PushMode(m);
    }

    public override int PopMode()
    {
        int oldMode = base.PopMode();
        Debug.WriteLine($"Push mode {modeNames[oldMode]}");
        return oldMode;
    }
}