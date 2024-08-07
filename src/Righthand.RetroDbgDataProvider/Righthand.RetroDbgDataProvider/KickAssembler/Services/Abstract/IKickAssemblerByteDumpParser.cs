using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract
{
    /// <summary>
    /// Provides parsing for byte-dump file.
    /// </summary>
    public interface IKickAssemblerByteDumpParser
    {
        /// <summary>
        /// Loads and parses byte dump file given by <param name="path">.</param>
        /// </summary>
        /// <param name="path">Path of the byte dump file.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A dictionary containing parsed byte dump file.</returns>
        ValueTask<FrozenDictionary<string, AssemblySegment>> LoadFileAsync(string path, CancellationToken ct = default);
    }
}
