using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Services.Abstract;

public interface IProjectServices
{
     /// <summary>
     /// Searches for matching files in project and all libraries.
     /// </summary>
     /// <param name="filter">Relative file path without extension or *, just the directory and file name part</param>
     /// <param name="extensions">File extension to watch for.</param>
     /// <param name="excludedFiles">Full file names to exclude from results</param>
     /// <returns>A dictionary with source as key and file names array as value. For project level files, the value is 'Project'.</returns>
     FrozenDictionary<string, FrozenSet<string>> GetMatchingFiles(string filter, FrozenSet<string> extensions, ICollection<string> excludedFiles);

     /// <summary>
     /// Collects segment names from entire project.
     /// </summary>
     /// <returns></returns>
     IEnumerable<string> CollectSegments();
}