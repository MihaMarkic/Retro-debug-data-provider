using Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;

public interface IKickAssemblerDbgParser
{
    ValueTask<C64Debugger> LoadFileAsync(string path, CancellationToken ct = default);
    ValueTask<C64Debugger> LoadContentAsync(string content, string path, CancellationToken ct = default);
}

