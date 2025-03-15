using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Services.Abstract;

/// <summary>
/// Provides project related services.
/// </summary>
public interface IProjectServices
{
     /// <summary>
     /// Searches for matching files in project and all libraries.
     /// </summary>
     /// <param name="relativeFilePath">Relative file's directory to either project or library</param>
     /// <param name="filter">Relative file path without extension or *, just the directory and file name part</param>
     /// <param name="extensions">File extension to watch for.</param>
     /// <param name="excludedFiles">Full file names to exclude from results</param>
     /// <returns>A dictionary with source as key and relative file names to <see cref="Path"/> array as value. For project level files, the value is 'Project'.</returns>
     FrozenDictionary<ProjectFileKey, FrozenSet<string>> GetMatchingFiles(string relativeFilePath, string filter, FrozenSet<string> extensions, ICollection<string> excludedFiles);
     /// <summary>
     /// Get directories matchig criteria.
     /// </summary>
     /// <param name="relativeFilePath"></param>
     /// <param name="filter"></param>
     /// <returns></returns>
     FrozenDictionary<ProjectFileKey, FrozenSet<string>> GetMatchingDirectories(string relativeFilePath, string filter);
     /// <summary>
     /// Collects preprocessor symbols from entire project.
     /// </summary>
     /// <returns></returns>
     FrozenSet<string> CollectPreprocessorSymbols();
     /// <summary>
     /// Collects default (global) scopes from all files in project.
     /// </summary>
     /// <returns></returns>
     ImmutableList<Scope> CollectDefaultScopes();
}

/// <summary>
/// A project file key combined from file origin an path.
/// </summary>
/// <param name="Origin"></param>
/// <param name="Path"></param>
public readonly record struct ProjectFileKey(ProjectFileOrigin Origin, string Path) : IEqualityComparer<ProjectFileKey>
{
    bool IEqualityComparer<ProjectFileKey>.Equals(ProjectFileKey x, ProjectFileKey y)
    {
        return x.Origin == y.Origin && x.Path.Equals(y.Path, OsDependent.FileStringComparison);
    }

    int IEqualityComparer<ProjectFileKey>.GetHashCode(ProjectFileKey obj)
    {
        return HashCode.Combine(Origin, Path);
    }
}

/// <summary>
/// File origin.
/// </summary>
public enum ProjectFileOrigin
{
    /// <summary>
    /// File is a part of project.
    /// </summary>
    Project,
    /// <summary>
    /// File is a part of a library.
    /// </summary>
    Library,
}