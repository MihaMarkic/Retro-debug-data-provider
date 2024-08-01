using System.Collections.Immutable;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;

public interface IKickAssemblerCompiler
{
    Task<(int ExitCode, ImmutableArray<CompilerError> Errors)> CompileAsync(string file, string projectDirectory, string outputDir, KickAssemblerCompilerSettings settings,
        Action<string> outputLine);
}