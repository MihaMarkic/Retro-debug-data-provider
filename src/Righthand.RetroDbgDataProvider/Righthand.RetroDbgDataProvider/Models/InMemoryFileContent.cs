namespace Righthand.RetroDbgDataProvider.Models;
/// <summary>
/// Represents the in memory file content.
/// </summary>
/// <param name="FilePath"></param>
/// <param name="Content"></param>
/// <param name="LastModified"></param>
public record InMemoryFileContent(string FilePath, string Content, DateTimeOffset LastModified);