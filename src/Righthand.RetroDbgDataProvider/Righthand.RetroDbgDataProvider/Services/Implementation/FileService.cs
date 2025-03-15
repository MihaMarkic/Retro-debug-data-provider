using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

/// <inheritdoc cref="IFileService"/>
public class FileService: IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly IOSDependent _iosDependent;

    /// <summary>
    /// Creates an instance of <see cref="FileService"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="iosDependent"></param>
    public FileService(ILogger<FileService> logger, IOSDependent iosDependent)
    {
        _logger = logger;
        _iosDependent = iosDependent;
    }
    /// <inheritdoc cref="IFileService"/>
    public bool FileExists(string path) => File.Exists(path);
    /// <inheritdoc cref="IFileService"/>
    public Stream OpenRead(string path) => File.OpenRead(path);

    /// <inheritdoc cref=""/>
    public async Task<string> ReadAllTextAsync(string path, ReadAllTextOption options = ReadAllTextOption.FixLineEndings,
        CancellationToken ct = default)
    {
        switch (options)
        {
            case ReadAllTextOption.FixLineEndings:
                await using (var stream = OpenRead(path))
                {
                    return await _iosDependent.ReadAllTextAndAdjustLineEndingsAsync(stream, ct);
                }
            case ReadAllTextOption.None:
                return await File.ReadAllTextAsync(path, ct);
            default:
                throw new Exception($"Unsupported ReadAllTextOption ${options}");
        }
    }
    /// <inheritdoc cref="IFileService"/>
    public DateTimeOffset GetLastWriteTime(string path) => File.GetLastWriteTime(path);
    
    /// <inheritdoc cref="IFileService"/>
    public Task WriteAllTextAsync(string path, string text, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, text, ct);
    /// <inheritdoc cref="IFileService"/>
    public void Delete(string path) => File.Delete(path);
}