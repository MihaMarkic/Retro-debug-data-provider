using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract
{
    /// <summary>
    /// Provides parsing for bytedump file.
    /// </summary>
    public interface IKickAssemblerByteDumpParser
    {
        ValueTask<FrozenDictionary<string, AssemblySegment>> LoadFileAsync(string path, CancellationToken ct = default);
    }
}
