using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Righthand.RetroDbgDataProvider;

/// <summary>
/// Provides support for reading file char by char.
/// </summary>
public static class FileReader
{
    /// <summary>
    /// Reads entire file char by char.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<char> ReadAllTextAsChars(Stream stream, [EnumeratorCancellation]CancellationToken ct = default)
    {
        const int BufferSize = 8096;
        using var reader = new StreamReader(stream, Encoding.UTF8, true, BufferSize, leaveOpen: true);
        using var charBuffer = BufferManager.GetBuffer<char>(BufferSize);

        int read;
        do
        {
            read = await reader.ReadAsync(charBuffer.Data, 0, BufferSize);
            for (int i = 0; i < read; i++)
            {
                yield return charBuffer.Data[i];
            }
        } while (read > 0);
    }
}