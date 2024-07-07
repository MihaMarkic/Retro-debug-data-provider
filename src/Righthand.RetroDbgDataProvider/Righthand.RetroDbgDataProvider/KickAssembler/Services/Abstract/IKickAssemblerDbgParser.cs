using Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;

/// <summary>
/// Provides parsing for .dgb file.
/// </summary>
public interface IKickAssemblerDbgParser
{
    /// <summary>
    /// Loads and parses given file
    /// </summary>
    /// <param name="path">Path to .dbg file</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    ValueTask<DbgData> LoadFileAsync(string path, CancellationToken ct = default);
    /// <summary>
    /// Parses given content.
    /// </summary>
    /// <param name="content">Content of .dbg file to parse</param>
    /// <param name="path">Origin file of the content</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    ValueTask<DbgData> LoadContentAsync(string content, string path, CancellationToken ct = default);
}

