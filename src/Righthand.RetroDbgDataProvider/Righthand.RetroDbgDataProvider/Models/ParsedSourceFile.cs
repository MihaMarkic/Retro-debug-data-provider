using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Models;

public record ParsedSourceFile(
    string FileName, 
    FrozenSet<string> ReferencedFiles,
    FrozenSet<string> InDefines,
    FrozenSet<string> OutDefines,
    DateTimeOffset LastModified,
    string? LiveContent);