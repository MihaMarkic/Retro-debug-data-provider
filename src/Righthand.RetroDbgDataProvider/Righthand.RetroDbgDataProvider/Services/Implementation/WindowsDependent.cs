using System.Text;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

/// <summary>
/// Windows OS dependent functions.
/// </summary>
public class WindowsDependent : IOSDependent
{
    /// <inheritdoc />
    public StringComparison FileStringComparison => StringComparison.CurrentCultureIgnoreCase;
    /// <inheritdoc />
    public StringComparer FileStringComparer => StringComparer.CurrentCultureIgnoreCase;
    /// <inheritdoc />
    public string ViceExeName => "x64sc.exe";
    /// <inheritdoc />
    public string JavaExeName => "java.exe";
    /// <inheritdoc />
    public string FileAppOpenName => "explorer.exe";
    /// <inheritdoc />
    public string NormalizePath(string path) => path.Replace('/', '\\');
    /// <inheritdoc />
    public async Task<string> ReadAllTextAndAdjustLineEndingsAsync(Stream stream, CancellationToken ct = default)
    {
        var sb = new StringBuilder((int)stream.Length);
        bool isLastR = false;
        await foreach (var c in FileReader.ReadAllTextAsChars(stream, ct))
        {
            if (!isLastR && c == '\n')
            {
                sb.Append("\r\n");
                isLastR = false;
            }
            else
            {
                isLastR = c == '\r';
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}