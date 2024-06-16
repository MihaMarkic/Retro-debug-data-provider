using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using System.Collections.Immutable;
using System.Globalization;
using System.Xml.Linq;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

public class KickAssemblerDbgParser(ILogger<KickAssemblerDbgParser> logger)
{
    public async Task<C64Debugger> LoadFileAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            throw new Exception($"File {path} does not exist");
        }
        string content;
        try
        {
            content = await File.ReadAllTextAsync(path, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to load KickAssembler debug files {path}");
            throw;
        }
        var result = await LoadContentAsync(content, ct);
        return result;
    }

    internal XElement GetElement(XElement root, string name) => root.Element(name) ?? new XElement(name);
    internal async ValueTask<C64Debugger> LoadContentAsync(string content, CancellationToken ct = default)
    {
        const string rootName = "C64debugger";
        try
        {
            var root = XElement.Parse(content);
            if (!rootName.Equals(root.Name.LocalName, StringComparison.Ordinal))
            {
                throw new Exception($"Excepted root element {rootName} but was {root.Name.LocalName}");
            }
            var source = GetElement(root, "Sources");
            var sourcesTask = ParseSources(source, ct);
            var segments = root.Elements("Segment");
            var segmentsTask = ParseSegments(segments);
            return new C64Debugger(
                (string?)root.Attribute("Version") ?? "?",
                await sourcesTask,
                await segmentsTask,
                ImmutableArray<Label>.Empty,
                ImmutableArray<Breakpoint>.Empty,
                ImmutableArray<Watchpoint>.Empty
                );
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to parser KickAssembler debug content");
            throw;
        }
    }

    internal int CountChars(string text, char c)
    {
        int count = 0;
        foreach (var t in text)
        {
            if (t == c)
            {
                count++;
            }
        }
        return count;
    }
    internal async ValueTask<ImmutableArray<Segment>> ParseSegments(IEnumerable<XElement> segments)
    {
        var tasks = segments.Select(s => ParseSegment(s));
        var builder = ImmutableArray.CreateBuilder<Segment>();
        foreach (var task in tasks)
        {
            builder.Add(await task.ConfigureAwait(false));
        }
        return builder.ToImmutable();
    }

    internal async ValueTask<Segment> ParseSegment(XElement segment)
    {
        var blocks = segment.Elements("Block");
        return new Segment(
            (string)(segment.Attribute("name") ?? throw new Exception("Segment missing a name attribute")), 
            await ParseBlocks(blocks));
    }

    internal async ValueTask<ImmutableArray<Block>> ParseBlocks(IEnumerable<XElement> blocks)
    {
        var tasks = blocks.Select(s => ParseBlock(s));
        var builder = ImmutableArray.CreateBuilder<Block>();
        foreach (var task in tasks)
        {
            builder.Add(await task.ConfigureAwait(false));
        }
        return builder.ToImmutable();
    }
    internal ValueTask<Block> ParseBlock(XElement block)
    {
        string lines = block.Value;
        var builder = ImmutableArray.CreateBuilder<BlockItem>(CountChars(lines, '\n') + 1);
        using (var reader = new StringReader(lines))
        {
            while (reader.ReadLine() is { } line)
            {
                builder.Add(ParseBlockItem(line));
            }
        }
        var result = new Block(
            (string)(block.Attribute("name") ?? throw new Exception("Block missing a name attribute")),
            builder.ToImmutable());
        return new ValueTask<Block>(result);
    }

    internal static BlockItem ParseBlockItem(string line)
    {
        var parts = line.Trim().Split(',');
        if (parts.Length != 7)
        {
            throw new Exception($"Block line '{line}' should have seven parts separated by comma");
        }
        return new BlockItem(
            Start: ParseHexText(parts[0], 4),
            End: ParseHexText(parts[1], 4),
            ParseFileLocationFragment(parts[2], parts[3], parts[4], parts[5], parts[6])
        );
    }

    internal static FileLocation ParseFileLocationFragment(string fileIndex, string line1, string col1, string line2, string col2)
    {
        return new FileLocation(
            SourceIndex: ParseInt(fileIndex, nameof(fileIndex)),
            Line1: ParseInt(line1, nameof(line1)),
            Col1: ParseInt(col1, nameof(col1)),
            Line2: ParseInt(line2, nameof(line2)),
            Col2: ParseInt(col2, nameof(col2))
        );
    }

    internal static int ParseInt(string text, string name) =>
        int.TryParse(text, out var result) ? result : throw new Exception($"Couldn't parse int value {name}");


    /// <summary>
    /// Parses hex text in form of '$XXXX'
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static ushort ParseHexText(string text, int digitsCount)
    {
        if (!text.StartsWith("$"))
        {
            throw new Exception($"Hex value {text} should start with char $");
        }
        var digits = text.AsSpan()[1..];
        if (digits.Length != digitsCount)
        {
            throw new Exception($"Expected {digitsCount} digits for hex value {text}");
        }
        if (!ushort.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
        {
            throw new Exception($"Text {text} is not a valid hex value");
        }
        return result;
    }

    internal ValueTask<ImmutableArray<Source>> ParseSources(XElement sources, CancellationToken ct = default)
    {
        string lines = sources.Value;
        var builder = ImmutableArray.CreateBuilder<Source>(CountChars(lines, '\n') + 1);
        using (var reader = new StringReader(lines))
        {
            while (reader.ReadLine() is { } line)
            {
                builder.Add(ParseSource(line));
            }
        }
        return new ValueTask<ImmutableArray<Source>>(builder.ToImmutable());
    }

    internal static Source ParseSource(string line)
    {
        var parts = line.Trim().Split(',');
        if (parts.Length != 2)
        {
            throw new Exception($"Source line '{line}' should have two parts separated by comma");
        }
        if (!int.TryParse(parts[0], out int index))
        {
            throw new Exception($"Source line '{line}' should have a valid number as first comma separated value");
        }
        const string kickAssJar = "KickAss.jar:";
        if (parts[1].StartsWith(kickAssJar, StringComparison.Ordinal))
        {
            return new Source(index, SourceOrigin.KickAss, parts[1].Substring((kickAssJar.Length)));
        }
        else
        {
            return new Source(index, SourceOrigin.User, parts[1]);
        }
    }
}

