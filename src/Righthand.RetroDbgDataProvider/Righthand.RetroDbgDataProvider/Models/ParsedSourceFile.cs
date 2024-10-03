namespace Righthand.RetroDbgDataProvider.Models;

public record ParsedSourceFile(string FileName, ImmutableHashSet<string> ReferencedFiles);