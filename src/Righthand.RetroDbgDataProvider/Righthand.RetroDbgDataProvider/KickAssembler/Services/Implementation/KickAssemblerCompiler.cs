using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

/// <inheritdoc />
public partial class KickAssemblerCompiler : IKickAssemblerCompiler
{
    [GeneratedRegex(
        "^\\s*\\((?<path>[a-zA-Z0-9:\\s_\\-\\\\/\\.]+)\\s+(?<line>\\d+):(?<row>\\d+)\\)\\s+Error:\\s+(?<error>.+)$",
        RegexOptions.Singleline)]
    private static partial Regex CompilerErrorRegex();

    [GeneratedRegex(@"^Error:\s*(?<text>.*)$", RegexOptions.Singleline)]
    private static partial Regex LastCompilerErrorTextRegex();

    [GeneratedRegex(@"^at\sline\s(?<line>\d+),\scolumn\s(?<column>\d+)\sin\s(?<file>[a-zA-Z0-9:\\s_\\-\\\\/\\.]+)$",
        RegexOptions.Singleline)]
    private static partial Regex LastCompilerErrorLocationRegex();

    private readonly ILogger<KickAssemblerCompiler> _logger;

    /// <summary>
    /// Initializes instance of <see cref="KickAssemblerCompiler"/>.
    /// </summary>
    /// <param name="logger"></param>
    public KickAssemblerCompiler(ILogger<KickAssemblerCompiler> logger)
    {
        _logger = logger;
    }
    /// <inheritdoc />
    public async Task<(int ExitCode, ImmutableArray<CompilerError> Errors)> CompileAsync(string file,
        string projectDirectory, string outputDir, KickAssemblerCompilerSettings settings,
        Action<string> outputLine)
    {
        const string bytedump = "bytedump.dmp";

        string javaExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "java.exe"
            : "java";
        string javaExe = settings.JavaPath is not null ? Path.Combine(settings.JavaPath, javaExeName) : javaExeName;
        // specific path to kick assembler binaries to overrides bundled ones 
        string kickAssemblerDirectory = settings.KickAssemblerPath ?? Path.Combine(Path.GetDirectoryName(typeof(KickAssemblerCompiler).Assembly.Location)!, "binaries", "KickAss");
        string kickAssemblerPath = Path.Combine($"\"{kickAssemblerDirectory}\"", "KickAss.jar");
        var processInfo = new ProcessStartInfo(javaExe,
            $"-jar {kickAssemblerPath} {file} -debugdump -bytedumpfile {bytedump} -define DEBUG -symbolfile -odir {outputDir}")
        {
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = projectDirectory,
        };
        var p = new Process
        {
            StartInfo = processInfo,
            EnableRaisingEvents = true,
        };
        if (p.Start())
        {
            var errorsBuilder = ImmutableArray.CreateBuilder<CompilerError>();
            // KickAssembler might show only error at the end of the output
            string? lastErrorText = null;
            while (!p.StandardOutput.EndOfStream)
            {
                string? line = await p.StandardOutput.ReadLineAsync();
                if (line is not null)
                {
                    outputLine(line);
                    if (lastErrorText is not null)
                    {
                        var lastErrorLocationMatch = LastCompilerErrorLocationRegex().Match(line);
                        if (lastErrorLocationMatch.Success)
                        {
                            // in case of last error, path is relative
                            // thus it is necessary to add directory in front as client expectes full paths
                            string path = Path.Combine(projectDirectory, lastErrorLocationMatch.Groups["file"].Value);
                            errorsBuilder.Add(new CompilerError(
                                int.Parse(lastErrorLocationMatch.Groups["line"].Value),
                                int.Parse(lastErrorLocationMatch.Groups["column"].Value),
                                lastErrorText,
                                path));
                        }
                        else
                        {
                            lastErrorText = null;
                            _logger.LogError("Expected last error location but couldn't match");
                        }
                    }
                    else
                    {
                        var errorMatch = CompilerErrorRegex().Match(line);
                        if (errorMatch.Success)
                        {
                            errorsBuilder.Add(new CompilerError(
                                int.Parse(errorMatch.Groups["line"].Value),
                                int.Parse(errorMatch.Groups["row"].Value),
                                errorMatch.Groups["error"].Value,
                                errorMatch.Groups["path"].Value));
                        }
                        else
                        {
                            var lastErrorTextMatch = LastCompilerErrorTextRegex().Match(line);
                            if (lastErrorTextMatch.Success)
                            {
                                lastErrorText = lastErrorTextMatch.Groups["text"].Value;
                            }
                        }
                    }
                }
            }

            await p.WaitForExitAsync();
            return (p.ExitCode, errorsBuilder.ToImmutable());
        }
        else
        {
            throw new Exception("Failed to start process");
        }
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// <see cref="KickAssemblerCompiler"/> specific settings.
/// </summary>
/// <param name="KickAssemblerPath">Explicit directory where KickAss.jar resides. When null, bundled binaries are used.</param>
/// <param name="JavaPath">Path to Java directory. If null, default environment path is used.</param>
public record KickAssemblerCompilerSettings(string? KickAssemblerPath, string? JavaPath = null);