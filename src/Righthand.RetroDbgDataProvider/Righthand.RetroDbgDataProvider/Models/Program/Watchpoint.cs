namespace Righthand.RetroDbgDataProvider.Models.Program;
/// <summary>
/// Hard code watchpoint.
/// </summary>
/// <param name="Address1">Start address</param>
/// <param name="Address2">End address</param>
/// <param name="Argument">Argument matching condition supported by emulator</param>
public record Watchpoint(ushort Address1, ushort? Address2, string? Argument);