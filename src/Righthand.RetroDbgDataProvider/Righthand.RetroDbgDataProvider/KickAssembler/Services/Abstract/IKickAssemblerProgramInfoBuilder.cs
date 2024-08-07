using Righthand.RetroDbgDataProvider.Models.Program;
using KickAss = Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;

/// <summary>
/// Provides conversion support from KickAssembler specific debug data to universal model.
/// </summary>
public interface IKickAssemblerProgramInfoBuilder
{
    /// <summary>
    /// Converts KickAssembler specific debug data to universal model.
    /// </summary>
    /// <param name="projectDirectory">Directory of the project.</param>
    /// <param name="dbgData">Debug data for the proejct.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns></returns>
    ValueTask<AssemblerAppInfo> BuildAppInfoAsync(string projectDirectory, KickAss.DbgData dbgData, CancellationToken ct = default);
}
