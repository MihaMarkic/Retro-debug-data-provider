namespace Righthand.RetroDbgDataProvider.Models;

public record InMemoryFileContent(string FilePath, string Content, DateTimeOffset LastModified);