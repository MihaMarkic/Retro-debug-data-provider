namespace Righthand.RetroDbgDataProvider.Models;

/// <summary>
/// Represents a character that triggered an operation.
/// </summary>
public enum TriggerChar
{
    /// <summary>
    /// Invalid
    /// </summary>
    Invalid,
    /// <summary>
    /// "
    /// </summary>
    DoubleQuote,
    /// <summary>
    /// ,
    /// </summary>
    Comma,
    /// <summary>
    /// .
    /// </summary>
    Dot,
    /// <summary>
    /// #
    /// </summary>
    Hash,
    /// <summary>
    /// =
    /// </summary>
    Assignment
}