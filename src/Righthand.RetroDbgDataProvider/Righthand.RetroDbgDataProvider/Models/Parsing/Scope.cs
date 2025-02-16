using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public class Scope: IScopeRange
{
    public static Scope Empty { get; } = new Scope([], [], null);
    public KickAssemblerParser.ScopeContext? Context { get; }
    public ImmutableList<Scope> Scopes { get; }
    public ImmutableList<IScopeElement> Elements { get; }
    public RangeInFile? Range => Context?.ToRange();
    public Scope(ImmutableList<Scope> scopes, ImmutableList<IScopeElement> elements, KickAssemblerParser.ScopeContext? context)
    {
        Context = context;
        Scopes = scopes;
        Elements = elements;
    }

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

public interface IScopeRange
{
    public RangeInFile? Range { get; }
}

public class ScopeBuilder
{
    public KickAssemblerParser.ScopeContext? Context { get; }
    public List<ScopeBuilder> Scopes { get; } = new();
    public List<IScopeElement> Elements { get; } = new();

    public ScopeBuilder(KickAssemblerParser.ScopeContext? context)
    {
        Context = context;
    }

    public Scope ToScope()
    {
        return new Scope(
            [..Scopes.Select(s => s.ToScope())],
            [..Elements],
            Context
        );
    }
}

public interface IScopeElement;

public abstract record ScopeElement<T>(T ParserContext) : IScopeElement
    where T : ParserRuleContext
{
    public RangeInFile? Range => ParserContext.ToRange();
}