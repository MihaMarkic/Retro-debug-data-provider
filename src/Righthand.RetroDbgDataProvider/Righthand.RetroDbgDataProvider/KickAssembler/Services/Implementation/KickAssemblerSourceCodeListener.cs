namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

public class KickAssemblerSourceCodeListener: KickAssemblerParserBaseListener
{
    private readonly HashSet<string> _referencedFiles = new();
    public ImmutableHashSet<string> ReferencedFiles => _referencedFiles.ToImmutableHashSet();
    public override void EnterPreprocessorDirective(KickAssemblerParser.PreprocessorDirectiveContext context)
    {
        var import = context.preprocessorImport();
        if (import is not null)
        {
            _referencedFiles.Add(import.STRING().GetText().Trim('"'));
        }
        else
        {
            var importIf = context.preprocessorImportIf();
            if (importIf is not null)
            {
                _referencedFiles.Add(importIf.STRING().GetText().Trim('"'));
            }
        }
        base.EnterPreprocessorDirective(context);
    }
}