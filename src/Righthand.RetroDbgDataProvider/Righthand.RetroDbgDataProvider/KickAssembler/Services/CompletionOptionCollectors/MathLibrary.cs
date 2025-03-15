using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

/// <summary>
/// Provides math library function definitions.
/// </summary>
/// <remarks>Generated with MathLibrary.netpad</remarks>
public static class MathLibrary
{
    /// <summary>
    /// Gets all math functions grouped by name.
    /// </summary>
    public static FrozenDictionary<string, FunctionDefinition> Functions { get; }
    /// <summary>
    /// Gets all function names.
    /// </summary>
    public static ImmutableArray<string> FunctionNames => Functions.Keys;

    static MathLibrary()
    {
        Functions = new Dictionary<string, FunctionDefinition>
        {
            { "abs", new("abs", ["x"]) },
            { "acos", new("acos", ["x"]) },
            { "asin", new("asin", ["x"]) },
            { "atan", new("atan", ["x"]) },
            { "atan2", new("atan2", ["y", "x"]) },
            { "cbrt", new("cbrt", ["x"]) },
            { "ceil", new("ceil", ["x"]) },
            { "cos", new("cos", ["r"]) },
            { "cosh", new("cosh", ["x"]) },
            { "exp", new("exp", ["x"]) },
            { "expm1", new("expm1", ["x"]) },
            { "floor", new("floor", ["x"]) },
            { "hypot", new("hypot", ["a", "b"]) },
            { "IEEEremainder", new("IEEEremainder", ["x", "y"]) },
            { "log", new("log", ["x"]) },
            { "log10", new("log10", ["x"]) },
            { "log1p", new("log1p", ["x"]) },
            { "max", new("max", ["x", "y"]) },
            { "min", new("min", ["x", "y"]) },
            { "mod", new("mod", ["a", "b"]) },
            { "pow", new("pow", ["x", "y"]) },
            { "random", new("random", [""]) },
            { "round", new("round", ["x"]) },
            { "signum", new("signum", ["x"]) },
            { "sin", new("sin", ["r"]) },
            { "sinh", new("sinh", ["x"]) },
            { "sqrt", new("sqrt", ["x"]) },
            { "tan", new("tan", ["r"]) },
            { "tanh", new("tanh", ["x"]) },
            { "toDegrees", new("toDegrees", ["r"]) },
            { "toRadians", new("toRadians", ["d"]) }
        }.ToFrozenDictionary();
    }

    /// <summary>
    /// Function definition.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Arguments"></param>
    public record FunctionDefinition(string Name, ImmutableList<string> Arguments);
}