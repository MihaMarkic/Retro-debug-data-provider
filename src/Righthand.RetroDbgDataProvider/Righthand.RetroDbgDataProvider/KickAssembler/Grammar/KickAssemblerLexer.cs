using Righthand.RetroDbgDataProvider.KickAssembler.PreprocessorCondition;
using Righthand.RetroDbgDataProvider.Models;
using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.KickAssembler;

partial class KickAssemblerLexer
{
    /// <summary>
    /// A set of #define symbols.
    /// </summary>
    public HashSet<string> DefinedSymbols { get; init; } = new();   
    private int? ModeOnDefaultEol { get; set; }
    /// <summary>
    /// A flag signaling whether file contains #importonce preprocessor directive.
    /// </summary>
    public bool IsImportOnce { get; private set; }
    /// <summary>
    /// A list of referenced files.
    /// </summary>
    public List<ReferencedFileInfo> ReferencedFiles { get; } = new();
    private bool IsDefined(string text) => PreprocessorConditionEvaluator.IsDefined(DefinedSymbols.ToFrozenSet(), text);

    /// <summary>
    /// Ordered list of preprocessor directives.
    /// </summary>
    public static readonly FrozenSet<string> PreprocessorDirectives =
        ["#define", "#elif", "#else", "#endif", "#if", "#import", "#importif", "#importonce", "#undef"];

    
    private static ImmutableArray<string> GenerateTokensSet(
#if NET90
        params ReadOnlySpan<int> tokens
#else
        params int[] tokens
#endif
    )
    {
        return [..tokens.Select(t => ruleNames[t])];
    }

    /// <inheritdoc />
    public override void PushMode(int m)
    {
        //Debug.WriteLine($"Push mode {modeNames[m]}");
        base.PushMode(m);
    }
    private void AddReferencedFileInfo(int tokenStartLine, int tokenStartColumn, string text)
    {
        var relativeFileName = text.Trim('"');
        string normalizedRelativeFileName = OsDependent.NormalizePath(relativeFileName);
        var info = new ReferencedFileInfo(tokenStartLine, tokenStartColumn, relativeFileName,
            normalizedRelativeFileName,
            DefinedSymbols.ToFrozenSet());
        ReferencedFiles.Add(info);
    }

    /// <inheritdoc />
    public override int PopMode()
    {
        int oldMode = base.PopMode();
        //Debug.WriteLine($"Push mode {modeNames[oldMode]}");
        return oldMode;
    }
}