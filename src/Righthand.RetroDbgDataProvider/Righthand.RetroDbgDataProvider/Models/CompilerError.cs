namespace Righthand.RetroDbgDataProvider.Models;
/// <summary>
/// Compiler error
/// </summary>
/// <param name="Line">Error line</param>
/// <param name="Column">Error column with line</param>
/// <param name="Text">Description</param>
/// <param name="Path">Path to source file where error originated</param>
public record CompilerError(int Line, int Column, string? Text, string Path);