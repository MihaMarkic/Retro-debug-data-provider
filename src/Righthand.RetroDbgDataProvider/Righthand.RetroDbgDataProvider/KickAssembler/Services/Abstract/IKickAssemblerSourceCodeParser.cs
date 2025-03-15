using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;

/// <summary>
/// Represents a KickAssemblerSourceCodeParser.
/// </summary>
public interface IKickAssemblerSourceCodeParser: ISourceCodeParser<KickAssemblerParsedSourceFile>
{
    
}