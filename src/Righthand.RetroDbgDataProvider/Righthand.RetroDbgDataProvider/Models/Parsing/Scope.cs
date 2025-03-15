using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Defines scope for semantic analysis elements.
/// </summary>
/// <remarks>
/// Elements such as for loop variables would be scoped inside for loop only and should not appear in code completion suggestions outside
/// </remarks>
public class Scope: IScopeRange
{
    /// <summary>
    /// Gets a static empty instance.
    /// </summary>
    public static Scope Empty { get; } = new Scope([], [], null);
    /// <summary>
    /// A <see cref="KickAssemblerParser.ScopeContext?"/>. Can be null.
    /// </summary>
    public KickAssemblerParser.ScopeContext? Context { get; }
    /// <summary>
    /// A list of children scopes.
    /// </summary>
    public ImmutableList<Scope> Scopes { get; }
    /// <summary>
    /// Gets elements within the scope.
    /// </summary>
    public ImmutableList<IScopeElement> Elements { get; }
    /// <summary>
    /// Gets range in the file.
    /// </summary>
    public RangeInFile? Range => Context?.ToRange();
    /// <summary>
    /// Creates an instance of <see cref="Scope"/>.
    /// </summary>
    /// <param name="scopes"></param>
    /// <param name="elements"></param>
    /// <param name="context"></param>
    public Scope(ImmutableList<Scope> scopes, ImmutableList<IScopeElement> elements, KickAssemblerParser.ScopeContext? context)
    {
        Context = context;
        Scopes = scopes;
        Elements = elements;
    }
    /// <summary>
    /// Evaluates whether scope contains given positon.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public bool ContainsPosition(int line, int column)
    {
        return Context is null || Range!.Value.IsInRange(line, column);
    }
    /// <summary>
    /// Iterates all elements, including those from nested scopes.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<(IScopeRange Scope, IScopeElement Element)> GetAllElementsInRange(int line, int column)
    {
        if (ContainsPosition(line, column))
        {
            foreach (var e in Elements)
            {
                yield return (this, e);
                switch (e)
                {
                    case For forElement:
                        foreach (var v in forElement.Variables)
                        {
                            yield return (forElement, v);
                        }
                        break;
                }
            }
        }

        foreach (var s in Scopes)
        {
            foreach (var e in s.GetAllElementsInRange(line, column))
            {
                yield return e;
            }
        }
    }
}
/// <summary>
/// Represents a scope rangee.
/// </summary>
public interface IScopeRange
{
    /// <summary>
    /// Range in file.
    /// </summary>
    public RangeInFile? Range { get; }
}

internal class ScopeBuilder
{
    internal KickAssemblerParser.ScopeContext? Context { get; }
    internal List<ScopeBuilder> Scopes { get; } = new();
    internal List<IScopeElement> Elements { get; } = new();

    internal ScopeBuilder(KickAssemblerParser.ScopeContext? context)
    {
        Context = context;
    }

    internal Scope ToScope()
    {
        return new Scope(
            [..Scopes.Select(s => s.ToScope())],
            [..Elements],
            Context
        );
    }
}
/// <summary>
/// Represents a <see cref="Scope"/> element.
/// </summary>
public interface IScopeElement;
/// <summary>
/// A base scope element.
/// </summary>
/// <param name="ParserContext"></param>
/// <typeparam name="T"></typeparam>
public abstract record ScopeElement<T>(T ParserContext) : IScopeElement
    where T : ParserRuleContext
{
    /// <summary>
    /// Gets range in file.
    /// </summary>
    public RangeInFile? Range => ParserContext.ToRange();
}