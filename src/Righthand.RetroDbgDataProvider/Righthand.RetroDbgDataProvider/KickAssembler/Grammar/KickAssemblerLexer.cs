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

    /// <summary>
    /// Ordered list of preprocessor directives.
    /// </summary>
    public static ImmutableArray<string> PreprocessorDirectives =
        ["#define", "#elif", "#else", "#endif", "#if", "#import", "#importif", "#importonce", "#undef"];
    /// <summary>
    /// Ordered list of directives.
    /// </summary>
    public static ImmutableArray<string> Directives = GenerateTokensSet(BREAK, BYTE, CONST,
        DEFINE, DISK, DOTBINARY, DOTC64, DOTCPU, DOTENCODING, DOTFILL,
        DOTFILLWORD, DOTLOHIFILL, DOTTEXT, DOTDWORD, ERRORIF, EVAL, FILE, FILEMODIFY, FOR, FUNCTION, IF, LABEL,
        MACRO, MODIFY, NAMESPACE, PC, PLUGIN, PRINT, PRINTNOW, PSEUDOCOMMAND, PSEUDOPC, RETURN, SEGMENT, SEGMENTDEF,
        SEGMENTOUT, STRUCT, VAR, WATCH, WHILE, DOTWORD, ZP);


    private static ImmutableArray<string> GenerateTokensSet(
#if NET90
        params ReadOnlySpan<int> tokens
#else
        params int[] tokens
#endif
    )
    {
        return [..tokens.Select(t => _LiteralNames[t]).Order()];
    }

    public override void PushMode(int m)
    {
        //Debug.WriteLine($"Push mode {modeNames[m]}");
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
        //Debug.WriteLine($"Push mode {modeNames[oldMode]}");
        return oldMode;
    }
}