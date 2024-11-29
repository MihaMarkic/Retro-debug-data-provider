using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

/// <inheritdoc />
public partial class KickAssemblerCompiler : IKickAssemblerCompiler
{

    private readonly ILogger<KickAssemblerCompiler> _logger;

    /// <summary>
    /// Initializes instance of <see cref="KickAssemblerCompiler"/>.
    /// </summary>
    /// <param name="logger"></param>
    public KickAssemblerCompiler(ILogger<KickAssemblerCompiler> logger)
    {
        _logger = logger;
    }

    internal static string CreateProcessArguments(string file,string outputDir, KickAssemblerCompilerSettings settings)
    {
        const string bytedump = "bytedump.dmp";
        // specific path to kick assembler binaries to overrides bundled ones 
        string kickAssemblerDirectory = settings.KickAssemblerPath ?? Path.Combine(Path.GetDirectoryName(typeof(KickAssemblerCompiler).Assembly.Location)!, "binaries", "KickAss");
        string kickAssemblerPath = Path.Combine($"\"{kickAssemblerDirectory}\"", "KickAss.jar");
        string? libDirs = !settings.LibDirs.IsDefaultOrEmpty ? $" {string.Join(' ', settings.LibDirs.Select(d => $"-libdir \"{d}\""))}"
            : null;
        string symbolsDefine = string.Join(" ", settings.DefineSymbols.Select(d => $"-define {d}"));
        return $"-jar {kickAssemblerPath} {file} -debugdump -bytedumpfile {bytedump} {symbolsDefine} -symbolfile -odir {outputDir}{libDirs}";
    }
    [GeneratedRegex(
        """
        ^\s*\((?<path>[a-zA-Z0-9:\s_\-\\\/\.]+)\s+(?<line>\d+):(?<column>\d+)\)\s+Error:\s+(?<error>.+)$
        """,
    RegexOptions.Singleline)]
    private static partial Regex CompilerErrorRegex();

    [GeneratedRegex(@"^Error:\s*(?<text>.*)$", RegexOptions.Singleline)]
    private static partial Regex LastCompilerErrorTextRegex();

    [GeneratedRegex(@"^at\sline\s(?<line>\d+),\scolumn\s(?<column>\d+)\sin\s(?<file>[a-zA-Z0-9:\\s_\\-\\\\/\\.]+)$",
        RegexOptions.Singleline)]
    private static partial Regex LastCompilerErrorLocationRegex();

    /// <inheritdoc />
    public async Task<(int ExitCode, ImmutableArray<(string Path, SyntaxError Error)> Errors)> CompileAsync(string file,
        string projectDirectory, string outputDir, KickAssemblerCompilerSettings settings,
        Action<string> outputLine)
    {

        string javaExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "java.exe"
            : "java";
        string javaExe = settings.JavaPath is not null ? Path.Combine(settings.JavaPath, javaExeName) : javaExeName;
        // specific path to kick assembler binaries to overrides bundled ones 
        string kickAssemblerDirectory = settings.KickAssemblerPath ?? Path.Combine(Path.GetDirectoryName(typeof(KickAssemblerCompiler).Assembly.Location)!, "binaries", "KickAss");
        string kickAssemblerPath = Path.Combine($"\"{kickAssemblerDirectory}\"", "KickAss.jar");
        string? libDirs = !settings.LibDirs.IsDefaultOrEmpty ? $" {string.Join(' ', settings.LibDirs.Select(d => $"-libdir {d}"))}"
            : null;
        var arguments = CreateProcessArguments(file, outputDir, settings);
        _logger.LogDebug("KickAssembler invoked as java {Java} and with arguments {Arguments}", javaExe, arguments);
        var processInfo = new ProcessStartInfo(javaExe, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
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
            var errorsBuilder = ImmutableArray.CreateBuilder<(string Path, SyntaxError Error)>();
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
                            // thus it is necessary to add directory in front as client expects full paths
                            string path = Path.Combine(projectDirectory, lastErrorLocationMatch.Groups["file"].Value);
                            int errorLine = int.Parse(lastErrorLocationMatch.Groups["line"].Value);
                            int errorColumn = int.Parse(lastErrorLocationMatch.Groups["column"].Value);
                            (int offset, int errorTextLength) = ExtrapolateErrorLength(lastErrorText);
                            errorsBuilder.Add((
                                path,
                                new SyntaxError(
                                    lastErrorText,
                                    null, errorLine-1, new SingleLineTextRange(errorColumn + offset, errorColumn + offset + errorTextLength),
                                    SyntaxErrorCompiledFileSource.Default)
                                ));
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
                            int errorLine = int.Parse(errorMatch.Groups["line"].Value);
                            int errorColumn = int.Parse(errorMatch.Groups["column"].Value);
                            string errorText = errorMatch.Groups["error"].Value.Trim();
                            (int offset, int errorTextLength) = ExtrapolateErrorLength(errorText);
                            errorsBuilder.Add((
                                errorMatch.Groups["path"].Value,
                                new SyntaxError(
                                    errorText,
                                    null,
                                    errorLine,
                                    new SingleLineTextRange(errorColumn + offset, errorColumn + offset + errorTextLength),
                                    SyntaxErrorCompiledFileSource.Default)
                            ));
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
            if (p.ExitCode != 0)
            {
                // when process returns an error, check whether there is output in StandardError stream
                // (failure to launch process stuff)
                var errorOutput = await p.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    throw new Exception(errorOutput);
                }
            }
            return (p.ExitCode, errorsBuilder.ToImmutable());
        }
        else
        {
            throw new Exception("Failed to start process");
        }
    }

    [GeneratedRegex("""
                    Can't\sopen\sfile:\s(?<fileName>[a-zA-Z0-9\._]+)
                    """, RegexOptions.Singleline)]
    private static partial Regex CanNotOpenFileRegex();
    internal static (int Offset, int Length) ExtrapolateErrorLength(string errorText)
    {
        Match m;
        if ((m = CanNotOpenFileRegex().Match(errorText)).Success)
        {
            return (0, m.Groups["fileName"].Length);
        }
        return (0, 1);
    }
}