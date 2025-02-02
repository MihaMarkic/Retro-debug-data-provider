using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Models;

/// <summary>
/// 
/// </summary>
/// <param name="TokenStartLine"></param>
/// <param name="TokenStartColumn"></param>
/// <param name="RelativeFilePath"></param>
/// <param name="NormalizedRelativeFilePath"></param>
/// <param name="InDefines"></param>
/// <param name="FullFilePath"></param>
/// <param name="InDefinesOverrideForImportOnce">
/// Define symbols when referencing an #importonce file.
/// From moment the file declares #importonce, the file with define symbols at that time is used,
/// and it should be used from that point forward.
/// </param>
public record ReferencedFileInfo(
    int TokenStartLine,
    int TokenStartColumn,
    string RelativeFilePath,
    string NormalizedRelativeFilePath,
    FrozenSet<string> InDefines,
    string? FullFilePath = null,
    FrozenSet<string>? InDefinesOverrideForImportOnce = null);