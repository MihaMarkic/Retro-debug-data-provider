namespace Righthand.RetroDbgDataProvider.Models.Program;
/// <summary>
/// Label in application.
/// </summary>
/// <param name="TextRange">Location in source file</param>
/// <param name="Name">Label name</param>
/// <param name="Address">Address of the label</param>
public record Label(TextRange TextRange, string Name, ushort Address);
