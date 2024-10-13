using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Models;

public record ReferencedFileInfo(int TokenStartLine, int TokenStartColumn, string RelativeFilePath, FrozenSet<string> InDefines,
    string? FullFilePath = null);