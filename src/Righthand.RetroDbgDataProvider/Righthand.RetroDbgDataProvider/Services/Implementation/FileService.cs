using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public class FileService: IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly IOSDependent _iosDependent;

    public FileService(ILogger<FileService> logger, IOSDependent iosDependent)
    {
        _logger = logger;
        _iosDependent = iosDependent;
    }

    public bool FileExists(string path) => File.Exists(path);
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

    public DateTimeOffset GetLastWriteTime(string path) => File.GetLastWriteTime(path);
    
    public Task WriteAllTextAsync(string path, string text, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, text, ct);

    public void Delete(string path) => File.Delete(path);
}