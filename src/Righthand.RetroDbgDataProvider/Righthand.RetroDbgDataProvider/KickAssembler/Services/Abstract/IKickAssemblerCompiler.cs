using System.Collections.Immutable;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;

/// <summary>
/// KickAssembler compiler service.
/// </summary>
public interface IKickAssemblerCompiler
{
    /// <summary>
    /// Runs KickAssembler compiler against the main file <paramref name="file"/> in project's directory.
    /// </summary>
    /// <param name="file">Main project's file.</param>
    /// <param name="projectDirectory">Directory containing the project.</param>
    /// <param name="outputDir">Output directory for compiler's output.</param>
    /// <param name="settings">Additional KickAssembler settings.</param>
    /// <param name="outputLine">Action that outputs compiler output.</param>
    /// <returns>A task that represents compiler result.</returns>
    Task<(int ExitCode, ImmutableArray<(string Path, SyntaxError Error)> Errors)> CompileAsync(string file, string projectDirectory, string outputDir, KickAssemblerCompilerSettings settings,
        Action<string> outputLine);
}

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// <see cref="KickAssemblerCompiler"/> specific settings.
/// </summary>
/// <param name="KickAssemblerPath">Explicit directory where KickAss.jar resides. When null, bundled binaries are used.</param>
/// <param name="LibDirs">An optional array or LibDir texts.</param>
/// <param name="JavaPath">Path to Java directory. If null, default environment path is used.</param>
public record KickAssemblerCompilerSettings(string? KickAssemblerPath, ImmutableArray<string> LibDirs, string? JavaPath = null);