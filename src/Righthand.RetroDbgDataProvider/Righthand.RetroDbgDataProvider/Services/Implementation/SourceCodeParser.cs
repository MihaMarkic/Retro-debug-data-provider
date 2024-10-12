using Righthand.RetroDbgDataProvider.Models;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public abstract class SourceCodeParser<T>
    where T: ParsedSourceFile
{
    /// <inheritdoc cref="ISourcecodeParser"/>
    public event EventHandler<FilesChangedEventArgs>? FilesChanged;

    protected void OnFilesChanged(FilesChangedEventArgs e) => FilesChanged?.Invoke(this, e);

    private void CompareFilesStatus(ImmutableDictionary<string, T>  current, 
        ImmutableDictionary<string, T> updated)
    {
        var newFiles = new Dictionary<string, T>();
    }
}