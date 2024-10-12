using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public abstract class SourceCodeParser<T>
    where T: ParsedSourceFile
{
    // ReSharper disable once InconsistentNaming
    protected IParsedFilesIndex<T> _allFiles;
    /// <inheritdoc cref="ISourcecodeParser"/>
    public event EventHandler<FilesChangedEventArgs>? FilesChanged;
    protected void OnFilesChanged(FilesChangedEventArgs e) => FilesChanged?.Invoke(this, e);

    protected SourceCodeParser(IParsedFilesIndex<T> allFiles)
    {
        _allFiles = allFiles;
    }

    /// <inheritdoc cref="ISourceCodeParser"/>
    public IParsedFilesIndex<T> AllFiles
    {
        get => _allFiles;
        protected set
        {
            if (!ReferenceEquals(_allFiles, value))
            {
                _allFiles = value;
                OnFilesChanged(FilesChangedEventArgs.Empty);
            }
        }
    }
    private void CompareFilesStatus(ImmutableDictionary<string, T>  current, 
        ImmutableDictionary<string, T> updated)
    {
        var newFiles = new Dictionary<string, T>();
    }
}