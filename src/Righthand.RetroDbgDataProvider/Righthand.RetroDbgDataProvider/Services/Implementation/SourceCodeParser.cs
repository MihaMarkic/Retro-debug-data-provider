using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Righthand.RetroDbgDataProvider.Models;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public abstract class SourceCodeParser<T> : DisposableObject
    where T : ParsedSourceFile
{
    /// <inheritdoc cref="ISourcecodeParser"/>
    public event EventHandler<FilesChangedEventArgs>? FilesChanged;
    public IParsedFilesIndex<T> AllFiles { get; private set; }
    /// <inheritdoc />
    public Task? ParsingTask { get; protected set; }
    protected SourceCodeParser(IParsedFilesIndex<T> allFiles)
    {
        AllFiles = allFiles;
    }
    private async ValueTask OnFilesChangedAsync(FilesChangedEventArgs e)
    {
        FilesChanged?.Invoke(this, e);
        await e.WaitAllClientTasksAsync();
    }

    protected async Task AssignNewFilesAsync(IParsedFilesIndex<T> newFiles, CancellationToken ct)
    {
        if (!ReferenceEquals(newFiles, AllFiles))
        {
            var changes = ExtractFileChanges(AllFiles, newFiles, ct);
            AllFiles = newFiles;
            await OnFilesChangedAsync(changes);
        }
    }

    /// <summary>
    /// Compares old and new state and returns differences.
    /// </summary>
    /// <param name="existingFiles"></param>
    /// <param name="newFiles"></param>
    /// <returns></returns>
    internal FilesChangedEventArgs ExtractFileChanges(IParsedFilesIndex<T> existingFiles, IParsedFilesIndex<T> newFiles,
        CancellationToken ct)
    {
        var addedFiles = new HashSet<string>(OsDependent.FileStringComparer);
        var modifiedFiles = new HashSet<string>(OsDependent.FileStringComparer);
        var deletedFiles = new HashSet<string>(existingFiles.Keys, OsDependent.FileStringComparer);

        foreach (var newFile in newFiles)
        {
            string path = newFile.AbsolutePath;
            var existingFile = existingFiles.GetValueOrDefault(path);
            if (existingFile is null)
            {
                addedFiles.Add(path);
            }
            else
            {
                if (!CompareFileSets(existingFile, newFile.Value))
                {
                    modifiedFiles.Add(path);
                }

                deletedFiles.Remove(path);
            }
        }

        return new FilesChangedEventArgs(addedFiles.ToFrozenSet(), modifiedFiles.ToFrozenSet(),
            deletedFiles.ToFrozenSet(), ct);
    }

    /// <summary>
    /// Compares two file sets for equality.
    /// </summary>
    /// <param name="existingSet"></param>
    /// <param name="newSet"></param>
    /// <returns>True when equal, false otherwise.</returns>
    internal static bool CompareFileSets(IImmutableParsedFileSet<T> existingSet, IImmutableParsedFileSet<T> newSet)
    {
        if (existingSet.Count != newSet.Count)
        {
            return false;
        }

        foreach (var newDefines in newSet.AllDefineSets)
        {
            if (!GetMatchingFrozenSet(existingSet.AllDefineSets, newDefines, out var existingDefines))
            {
                return false;
            }

            var existingParsedFile = existingSet.GetFile(existingDefines);
            var newParsedFile = newSet.GetFile(newDefines);
            if (!ReferenceEquals(existingParsedFile, newParsedFile))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Searches for matching set and if one is found, returns it in <paramref name="matching"/>.
    /// </summary>
    /// <param name="sets"></param>
    /// <param name="searchSet"></param>
    /// <param name="matching"></param>
    /// <returns>True when <paramref name="matching"/> is found, false otherwise.</returns>
    internal static bool GetMatchingFrozenSet(ImmutableArray<FrozenSet<string>> sets, FrozenSet<string> searchSet,
        [NotNullWhen(true)] out FrozenSet<string>? matching)
    {
        foreach (var s in sets)
        {
            if (s.SetEquals(searchSet))
            {
                matching = s;
                return true;
            }
        }

        matching = null;
        return false;
    }
}