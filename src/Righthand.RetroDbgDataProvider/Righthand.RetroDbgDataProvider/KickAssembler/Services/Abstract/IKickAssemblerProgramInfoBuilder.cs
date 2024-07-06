using Righthand.RetroDbgDataProvider.Models.Program;
using KickAss = Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;

public interface IKickAssemblerProgramInfoBuilder
{
    ValueTask<AssemblerAppInfo> BuildAppInfoAsync(KickAss.C64Debugger dbgData, CancellationToken ct = default);
}
