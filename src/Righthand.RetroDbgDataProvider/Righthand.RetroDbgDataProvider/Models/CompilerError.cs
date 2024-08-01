namespace Righthand.RetroDbgDataProvider.Models;

public record CompilerError(int Line, int Column, string? Text, string Path);