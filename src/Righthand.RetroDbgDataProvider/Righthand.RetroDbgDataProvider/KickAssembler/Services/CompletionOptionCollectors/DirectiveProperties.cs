﻿using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

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
                ("c64", [new FileDirectiveValueType(".c64")]),
                ("text", [new FileDirectiveValueType(".txt")]),
                ("binary", [new FileDirectiveValueType(".bin")]),
                ("source", [new FileDirectiveValueType(".asm")])
            )
        );
        temp.AddWithoutTypeEnumerableValues(".encoding", "ascii", "petscii_mixed", "petscii_upper", "screencode_mixed", "screencode_upper");
        temp.AddWithoutType(".text", ".fill", ".fillword", ".lohifill", ".cpu",
            "_6502NoIllegals", "_6502", "dtv", "_65c02", ".assert", ".asserterror", ".print", ".printnow", ".var",
            ".const", ".if", "else", ".errorif", ".eval", ".enum", ".for", ".while", ".struct", ".defint", ".function",
            ".return", ".macro", ".pseudocommand", ".pseudopc", ".namespace", ".segment", ".segmentdef", ".segmentout",
            ".modify", ".fileModify", ".plugin", ".label", ".file", ".disk", ".pc", ".break", ".watch", ".zp");

        Data = temp.ToFrozenDictionary();
    }

    private static void AddWithType(this Dictionary<string, Directive> source, string directiveName, FrozenDictionary<string, FrozenSet<DirectiveValueType>> valueTypes)
    {
        source.Add(directiveName, new DirectiveWithType(directiveName, valueTypes));   
    }
    private static void AddWithoutType(this Dictionary<string, Directive> source, string directiveName, FrozenSet<DirectiveValueType> directiveValueTypes)
    {
        source.Add(directiveName, new DirectiveWithoutType(directiveName, directiveValueTypes));   
    }
    private static void AddWithoutType(this Dictionary<string, Directive> source, params ImmutableArray<string> directiveNames)
    {
        foreach (var dn in directiveNames)
        {
            source.Add(dn, new DirectiveWithoutType(dn, []));
        }
    }
    private static void AddWithoutTypeEnumerableValues(this Dictionary<string, Directive> source, string directiveName, params FrozenSet<string> values)
    {
        source.AddWithoutType(directiveName, [..values.Select(v => new EnumerableDirectiveValueType(v))]);   
    }
    private static FrozenDictionary<string, FrozenSet<DirectiveValueType>> CreateTypeValues<T>(params (string Name, FrozenSet<T> ValueTypes)[] types)
        where T: DirectiveValueType
    {
        return types.ToFrozenDictionary(v => v.Name, v => v.ValueTypes.OfType<DirectiveValueType>().ToFrozenSet(), StringComparer.OrdinalIgnoreCase);
    }
    
    public static bool TryGetDirective(string key, [NotNullWhen(true)]out Directive? directive) => Data.TryGetValue(key, out directive);

    /// <summary>
    /// Returns directives that match the common root, but not equals.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public static FrozenSet<string> GetDirectives(string root)
    {
        return [..Data.Keys.Where(k => k.Length > root.Length && k.StartsWith(root, StringComparison.OrdinalIgnoreCase))];
    }

    public static ImmutableArray<string> AllDirectives => Data.Keys;

    public static FrozenSet<DirectiveValueType>? GetValueTypes(string directiveName, string? directiveType)
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
                    else
                    {
                        // if type is not specified, return all values
                        return [..directiveWithType.ValueTypes.SelectMany(vt => vt.Value)];
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
public record EnumerableDirectiveValueType(string Value) : DirectiveValueType();