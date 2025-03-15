using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

/// <summary>
/// Color constants.
/// </summary>
public static class ColorConstants
{
    /// <summary>
    /// Gets all C64 color strings.
    /// </summary>
    public static FrozenSet<string> Colors = ["BLACK", "WHITE", "RED", "CYAN", "PURPLE", "GREEN", "BLUE", "YELLOW", "ORANGE", "BROWN","LIGHT_RED", "DARK_GRAY",
        "DARK_GREY", "GRAY", "GREY", "LIGHT_GREEN", "LIGHT_BLUE", "LIGHT_GRAY", "LIGHT_GREY"];
}