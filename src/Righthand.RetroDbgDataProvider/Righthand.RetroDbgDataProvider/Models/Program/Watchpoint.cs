namespace Righthand.RetroDbgDataProvider.Models.Program;

public record Watchpoint(ushort Address1, ushort? Address2, string? Argument);