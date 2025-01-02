using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

/// <summary>
/// Defines meta data for directive properties.
/// </summary>
public static class DirectiveProperties
{
    private static readonly FrozenDictionary<string, Directive> Data;

    static DirectiveProperties()
    {
        var temp = new Dictionary<string, Directive>(StringComparer.OrdinalIgnoreCase);
        temp.AddWithType(".import", CreateTypeValues(
                ("c64", [new FileDirectiveValueType("prg")]),
                ("text", [new FileDirectiveValueType("txt")]),
                ("binary", [new FileDirectiveValueType("bin")])
            )
        );
        Data = temp.ToFrozenDictionary();
    }

    private static void AddWithType(this Dictionary<string, Directive> source, string directiveName, FrozenDictionary<string, FrozenSet<DirectiveValueType>> valueTypes)
    {
        source.Add(directiveName, new DirectiveWithType(directiveName, valueTypes));   
    }
    private static FrozenDictionary<string, FrozenSet<DirectiveValueType>> CreateTypeValues<T>(params (string Name, FrozenSet<T> ValueTypes)[] types)
        where T: DirectiveValueType
    {
        return types.ToFrozenDictionary(v => v.Name, v => v.ValueTypes.OfType<DirectiveValueType>().ToFrozenSet(), StringComparer.OrdinalIgnoreCase);
    }

    public static FrozenSet<DirectiveValueType>? GetValueType(string directiveName, string? directiveType)
    {
        if (Data.TryGetValue(directiveName, out var directive))
        {
            switch (directive)
            {
                case DirectiveWithType directiveWithType:
                    if (!string.IsNullOrEmpty(directiveType))
                    {
                        if (directiveWithType.ValueTypes.TryGetValue(directiveType, out var directiveValues))
                        {
                            return directiveValues;
                        }
                    }

                    break;
                case DirectiveWithoutType directiveWithoutType:
                    return directiveWithoutType.ValueTypes;
            }
        }

        return null;
    }
}

public abstract record Directive(string Name);

public record DirectiveWithType(string Name, FrozenDictionary<string, FrozenSet<DirectiveValueType>> ValueTypes) : Directive(Name);

public record DirectiveWithoutType(string Name, FrozenSet<DirectiveValueType> ValueTypes) : Directive(Name);


public abstract record DirectiveValueType();

public record FileDirectiveValueType(string FileExtension) : DirectiveValueType();