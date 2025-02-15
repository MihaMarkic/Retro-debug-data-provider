using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
/// <summary>
/// Provides math library function definitions.
/// </summary>
/// <remarks>Generated with MathLibrary.netpad</remarks>
public static class MathLibrary
{
    public static FrozenDictionary<string, Function> Functions { get; }
    public static ImmutableArray<string> FunctionNames => Functions.Keys;

    static MathLibrary()
    {
        Functions = new Dictionary<string, Function>
        {
            { "abs", new ("abs", false, ["x"]) },
            { "acos", new ("acos", false, ["x"]) },
            { "asin", new ("asin", false, ["x"]) },
            { "atan", new ("atan", false, ["x"]) },
            { "atan2", new ("atan2", false, ["y", "x"]) },
            { "cbrt", new ("cbrt", false, ["x"]) },
            { "ceil", new ("ceil", false, ["x"]) },
            { "cos", new ("cos", false, ["r"]) },
            { "cosh", new ("cosh", false, ["x"]) },
            { "exp", new ("exp", false, ["x"]) },
            { "expm1", new ("expm1", false, ["x"]) },
            { "floor", new ("floor", false, ["x"]) },
            { "hypot", new ("hypot", false, ["a", "b"]) },
            { "IEEEremainder", new ("IEEEremainder", false, ["x", "y"]) },
            { "log", new ("log", false, ["x"]) },
            { "log10", new ("log10", false, ["x"]) },
            { "log1p", new ("log1p", false, ["x"]) },
            { "max", new ("max", false, ["x", "y"]) },
            { "min", new ("min", false, ["x", "y"]) },
            { "mod", new ("mod", false, ["a", "b"]) },
            { "pow", new ("pow", false, ["x", "y"]) },
            { "random", new ("random", false, [""]) },
            { "round", new ("round", false, ["x"]) },
            { "signum", new ("signum", false, ["x"]) },
            { "sin", new ("sin", false, ["r"]) },
            { "sinh", new ("sinh", false, ["x"]) },
            { "sqrt", new ("sqrt", false, ["x"]) },
            { "tan", new ("tan", false, ["r"]) },
            { "tanh", new ("tanh", false, ["x"]) },
            { "toDegrees", new ("toDegrees", false, ["r"]) },
            { "toRadians", new ("toRadians", false, ["d"]) }
        }.ToFrozenDictionary();
    }
}
