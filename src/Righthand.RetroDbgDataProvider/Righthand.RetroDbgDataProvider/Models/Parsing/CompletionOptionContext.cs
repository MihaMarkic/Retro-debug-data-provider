using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Provides context services for completion option.
/// </summary>
/// <param name="ProjectServices">Access to project and library level files</param>
/// <param name="SourceFile">Provides access to source file that initiated</param>
public readonly record struct CompletionOptionContext(IProjectServices ProjectServices, IParsedSourceFile SourceFile);