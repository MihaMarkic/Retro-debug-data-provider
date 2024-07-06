using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Models.Program;

/// <summary>
/// Presents assembler based application data.
/// </summary>
/// <param name="SourceFiles">Source files with path as key.</param>
/// <param name="Segments">Segments with name as key.</param>
public record AssemblerAppInfo(
    FrozenDictionary<SourceFilePath, SourceFile> SourceFiles,
    FrozenDictionary<string, Segment> Segments);