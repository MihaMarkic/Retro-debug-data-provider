using System.Buffers;
using System.Text;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public abstract class NonWindowsDependent
{
    public StringComparison FileStringComparison => StringComparison.CurrentCulture;
    public StringComparer FileStringComparer => StringComparer.CurrentCulture;
    public string ViceExeName => "x64sc";
    public string JavaExeName => "java";
    public string NormalizePath(string path) => path.Replace('\\', '/');
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