using System.Collections.Frozen;
using Antlr4.Runtime;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

public class KickAssemblerParserListener: KickAssemblerParserBaseListener
{
   private readonly Dictionary<IToken, string> _fileReferences = new();
   public FrozenDictionary<IToken, string> FileReferences => _fileReferences.ToFrozenDictionary();
   public override void EnterPreprocessorImport(KickAssemblerParser.PreprocessorImportContext context)
   {
      var fileReference = context.fileReference;
      if (fileReference is not null)
      {
         _fileReferences.Add(fileReference, fileReference.Text);
      }
      base.EnterPreprocessorImport(context);
   }
}