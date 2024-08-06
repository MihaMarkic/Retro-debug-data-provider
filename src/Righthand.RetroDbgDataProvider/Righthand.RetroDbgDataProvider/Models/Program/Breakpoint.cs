namespace Righthand.RetroDbgDataProvider.Models.Program;

/// <summary>
/// Hard-coded breakpoint.
/// </summary>
/// <param name="Address">Address of the breakpoint.</param>
/// <param name="Argument">Condition syntax supported by the emulator for the breakpoint.</param>
public record Breakpoint(ushort Address, string? Argument);