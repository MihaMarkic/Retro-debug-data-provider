using System.Buffers;
using System.Text;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

/// <summary>
/// Commmon non windows OS dependent code.
/// </summary>
public abstract class NonWindowsDependent
{
    /// <inheritdoc />
    public StringComparison FileStringComparison => StringComparison.CurrentCulture;
    /// <inheritdoc />
    public StringComparer FileStringComparer => StringComparer.CurrentCulture;
    /// <inheritdoc />
    public string ViceExeName => "x64sc";
    /// <inheritdoc />
    public string JavaExeName => "java";
    /// <inheritdoc />
    public string NormalizePath(string path) => path.Replace('\\', '/');
    /// <inheritdoc />
    public async Task<string> ReadAllTextAndAdjustLineEndingsAsync(Stream stream, CancellationToken ct = default)
    {
        var sb = new StringBuilder((int)stream.Length);
        bool isLastR = false;
        await foreach (var c in FileReader.ReadAllTextAsChars(stream, ct))
        {
            if (isLastR)
            {
                if (c == '\n')
                {
                    sb.Append('\n');
                    isLastR = false;
                }
                else
                {
                    sb.Append('\r');
                    isLastR = c == '\r';
                }
            }
            else if (c == '\r')
            {
                isLastR = true;
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}