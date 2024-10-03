using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.Services.Abstract;
/// <summary>
/// Provides parsing of the source code for the entire project.
/// </summary>
public interface ISourcecodeParser
{
    /// <summary>
    /// Starts initial parsing of entire project.
    /// </summary>
    /// <param name="projectDirectory"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task InitialParseAsync(string projectDirectory, CancellationToken ct);
    /// <summary>
    /// Trigger reparsing of source code
    /// </summary>
    /// <param name="changedFiles"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task ParseAsync(ImmutableArray<string> changedFiles, CancellationToken ct = default);
}