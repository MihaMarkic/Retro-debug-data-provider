using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Program;
using KickAss = Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

/// <summary>
/// Creates a new instance.
/// </summary>
/// <param name="_logger"></param>
public class KickAssemblerProgramInfoBuilder(ILogger<KickAssemblerProgramInfoBuilder> _logger)
: IKickAssemblerProgramInfoBuilder
{
    public async ValueTask<AssemblerAppInfo> BuildAppInfoAsync(KickAss.C64Debugger dbgData, CancellationToken ct = default)
    {
        var labels = CreateLabels(dbgData.Labels);
        // maps labels by file index
        var labelsMap = labels
            .GroupBy(l => l.Original.FileLocation.SourceIndex, l => l.New)
            .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());
        var breakpoints = dbgData.Breakpoints.Select(CreateBreakpoint).ToImmutableArray();
        // maps breakpoints by segment name
        var breakpointsMap = breakpoints
            .GroupBy(g => g.Original.SegmentName, g => g.New)
            .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());
        var watchpoints = dbgData.Watchpoints.Select(CreateWatchpoint).ToImmutableArray();
        // maps watchpoints by segment name
        var watchpointsMap = watchpoints
            .GroupBy(g => g.Original.SegmentName, g => g.New)
            .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());
        var allSegments = dbgData.Segments.Select(
            s => CreateSegment(
                s,
                breakpointsMap.GetArrayOrEmpty(s.Name),
                watchpointsMap.GetArrayOrEmpty(s.Name))
            ).ToImmutableArray();
        // maps block items by file index
        var blockItemsMap = allSegments.SelectMany(s => s.LinkedBlockItems)
            .GroupBy(
                li => li.Original.FileLocation.SourceIndex,
                li => li.New)
            .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());
        var sourceFiles = await BuildSourceFilesAsync(dbgData.Sources, labelsMap, blockItemsMap, dbgData.Path, ct);
        return new AssemblerAppInfo(
            sourceFiles.ToFrozenDictionary(f => f.Path),
            allSegments.Select(ls => ls.Segment)
                .ToFrozenDictionary(s => s.Name)
            );
    }

    internal (
        Segment Segment,
        ImmutableArray<LinkedOriginal<BlockItem, KickAss.BlockItem>> LinkedBlockItems
        )
        CreateSegment(KickAss.Segment source, ImmutableArray<Breakpoint> breakpoints, ImmutableArray<Watchpoint> watchpoints)
    {
        var linkedBlocks = source.Blocks.Select(CreateBlock).ToImmutableArray();
        var segment = new Segment(
            source.Name,
            [.. linkedBlocks.Select(l => l.Block)],
            breakpoints, watchpoints);
        var allLinkedBlockItems = linkedBlocks.SelectMany(lb => lb.LinkedItems).ToImmutableArray();
        return (segment, allLinkedBlockItems);
    }

    internal (Block Block, ImmutableArray<LinkedOriginal<BlockItem, KickAss.BlockItem>> LinkedItems)
        CreateBlock(KickAss.Block source)
    {
        var items = source.Items
            .Select(CreateBlockItem)
            .ToImmutableArray();
        return (
            new Block(source.Name, [.. items.Select(i => i.New)]),
            items);
    }


    internal ImmutableArray<LinkedOriginal<Label, KickAss.Label>> CreateLabels(ImmutableArray<KickAss.Label> source)
    {
        return source.Select(l =>
                LinkedOriginalBuilder.Create(CreateLabel(l), l))
            .ToImmutableArray();
    }
    internal async ValueTask<ImmutableArray<SourceFile>> BuildSourceFilesAsync(ImmutableArray<KickAss.Source> sourceFiles,
        FrozenDictionary<int, ImmutableArray<Label>> labelsMap, FrozenDictionary<int, ImmutableArray<BlockItem>> blockItemsMap,
        string rootDirectory,
        CancellationToken ct)
    {
        return sourceFiles
            .Select((s, i) => BuildSourceFile(
                s, 
                labelsMap.GetArrayOrEmpty(i), 
                blockItemsMap.GetArrayOrEmpty(i),
                rootDirectory))
            .ToImmutableArray();
    }
    internal LinkedOriginal<Breakpoint, KickAss.Breakpoint> CreateBreakpoint(KickAss.Breakpoint source)
        => new(new Breakpoint(source.Address, source.Argument), source);
    internal LinkedOriginal<Watchpoint, KickAss.Watchpoint> CreateWatchpoint(KickAss.Watchpoint source)
    => new(new Watchpoint(source.Address1, source.Address1, source.Argument), source);
    internal LinkedOriginal<BlockItem, KickAss.BlockItem> CreateBlockItem(KickAss.BlockItem source)
        => LinkedOriginalBuilder.Create(new BlockItem(
            source.Start,
            source.End,
            FileLocationToTextRange(source.FileLocation)), source);

    internal Label CreateLabel(KickAss.Label source)
        => new Label(
            FileLocationToTextRange(source.FileLocation),
            source.Name,
            source.Address);

    internal TextRange FileLocationToTextRange(KickAss.FileLocation source)
        => new TextRange(
            new TextCursor(source.Line1, source.Col1),
            new TextCursor(source.Line2, source.Col2));

    internal SourceFile BuildSourceFile(KickAss.Source source,
        ImmutableArray<Label> labels, ImmutableArray<BlockItem> blockItems,
        string rootDirectory)
    {
        return new SourceFile(
            SourceFilePath.Create(rootDirectory, source.Path),
            labels.ToFrozenDictionary(l => l.Name),
            blockItems
            );
    }
}

internal static class FrozenDirectoryExtensions
{
    public static ImmutableArray<TValue> GetArrayOrEmpty<TKey, TValue>(this FrozenDictionary<TKey, ImmutableArray<TValue>> source, TKey key)
        where TKey: notnull
        => source.TryGetValue(key, out var value) ? value : ImmutableArray<TValue>.Empty;
}