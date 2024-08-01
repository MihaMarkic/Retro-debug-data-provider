using Righthand.RetroDbgDataProvider.Models.Program;
using KickAss = Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;

/// <summary>
/// Creates a new instance.
/// </summary>
public interface IKickAssemblerProgramInfoBuilder
{
    ValueTask<AssemblerAppInfo> BuildAppInfoAsync(string projectDirectory, KickAss.DbgData dbgData, CancellationToken ct = default);
}
