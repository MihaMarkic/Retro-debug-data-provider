using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.Models;

namespace Righthand.RetroDbgDataProvider.Services.Abstract;
/// <summary>
/// Provides parsing of the source code for the entire project.
/// </summary>
public interface ISourcecodeParser
{
    /// <summary>
    /// Notifies that <see cref="AllFiles"/> has changed.
    /// </summary>
    event EventHandler? AllFilesChanged;
    /// <summary>
    /// Parsed data for all files in current state.
    /// </summary>
    /// <remarks><see cref="AllFilesChanged"/> is triggered on each change.</remarks>
    ImmutableDictionary<string, KickAssemblerParsedSourceFile> AllFiles { get; }
    /// <summary>
    /// Starts initial parsing of entire project.
    /// </summary>
    /// <param name="projectDirectory"></param>
    /// <param name="inMemoryFilesContent"></param>
    /// <param name="inDefines"></param>
    /// <param name="libraryDirectories"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task InitialParseAsync(string projectDirectory,
        FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories, CancellationToken ct = default);
    /// <summary>
    /// Trigger reparsing of source code
    /// </summary>
    /// <param name="inMemoryFilesContent"></param>
    /// <param name="inDefines"></param>
    /// <param name="libraryDirectories"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task ParseAsync(FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories, CancellationToken ct = default);

    /// <summary>
    /// When parsing in progress, it stops it. Otherwise has no effect.
    /// </summary>
    /// <returns></returns>
    Task StopAsync();
}