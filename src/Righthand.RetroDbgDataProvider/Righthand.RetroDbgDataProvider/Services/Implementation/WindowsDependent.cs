using System.Text;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public class WindowsDependent : IOSDependent
{
    public StringComparison FileStringComparison => StringComparison.CurrentCultureIgnoreCase;
    public StringComparer FileStringComparer => StringComparer.CurrentCultureIgnoreCase;
    public string ViceExeName => "x64sc.exe";
    public string JavaExeName => "java.exe";
    public string FileAppOpenName => "explorer.exe";
    public string NormalizePath(string path) => path.Replace('/', '\\');
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