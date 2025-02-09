using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Services.Abstract;

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
     FrozenDictionary<ProjectFileKey, FrozenSet<string>> GetMatchingDirectories(string relativeFilePath, string filter);

     /// <summary>
     /// Collects segment names from entire project.
     /// </summary>
     /// <returns>All segment names</returns>
     IEnumerable<string> CollectSegments();
     /// <summary>
     /// Collects preprocessor symbols from entire project.
     /// </summary>
     /// <returns></returns>
     FrozenSet<string> CollectPreprocessorSymbols();
     /// <summary>
     /// Collects label definitions.
     /// </summary>
     /// <returns></returns>
     ImmutableList<Label> CollectLabels();
     /// <summary>
     /// Collects variable definitions.
     /// </summary>
     /// <returns></returns>
     ImmutableList<string> CollectVariables();
     /// <summary>
     /// Collects constant definitions.
     /// </summary>
     /// <returns></returns>
     ImmutableList<Constant> CollectConstants();
     /// <summary>
     /// Collects enum value definitions.
     /// </summary>
     /// <returns></returns>
     ImmutableList<EnumValues> CollectEnumValues(); 
}

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

public enum ProjectFileOrigin
{
    Project,
    Library,
}