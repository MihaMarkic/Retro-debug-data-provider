using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Program;
using KickAss = Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

/// <summary>
/// Creates a new instance.
/// </summary>
/// <param name="_logger"></param>
public class KickAssemblerProgramInfoBuilder(ILogger<KickAssemblerProgramInfoBuilder> _logger)
{
    public async ValueTask<AssemblerAppInfo> BuildAppInfo(KickAss.C64Debugger dbgData, CancellationToken ct = default)
    {
        var labels = CreateLabels(dbgData.Labels);
        // maps labels by file index
        var labelsMap = labels.ToFrozenDictionary(
            l => l.Original.FileLocation.SourceIndex,
            l => l.New);
        var allSegments = dbgData.Segments.Select(CreateSegment).ToImmutableArray();
        // maps block items by file index
        var blockItemsMap = allSegments.SelectMany(s => s.LinkedBlockItems)
            .ToFrozenDictionary(
                li => li.Original.FileLocation.SourceIndex,
                li => li.New);
        var sourceFiles = BuildSourceFiles(dbgData, ct);
        var linkedSegments = dbgData.Segments.Select(CreateSegment).ToImmutableArray();
        return new AssemblerAppInfo(
            (await sourceFiles).ToFrozenDictionary(f => f.Path, f => f),
            [..linkedSegments.Select(ls => ls.Segment)]
            );
    }

    internal (
        Segment Segment, 
        ImmutableArray<LinkedOriginal<BlockItem, KickAss.BlockItem>> LinkedBlockItems
        )
        CreateSegment(KickAss.Segment source)
    {
        var linkedBlocks = source.Blocks.Select(CreateBlock).ToImmutableArray();
        var segment = new Segment(source.Name, [..linkedBlocks.Select(l => l.Block)]);
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
            new Block(source.Name, [..items.Select(i => i.New)]), 
            items);
    }

    internal LinkedOriginal<BlockItem, KickAss.BlockItem> CreateBlockItem(KickAss.BlockItem source)
        => LinkedOriginalBuilder.Create(new BlockItem(
            source.Start,
            source.End,
            FileLocationToTextRange(source.FileLocation)), source);

    internal ImmutableArray<LinkedOriginal<Label, KickAss.Label>> CreateLabels(ImmutableArray<KickAss.Label> source)
    {
        return source.Select(l => 
                LinkedOriginalBuilder.Create(CreateLabel(l), l))
            .ToImmutableArray();
    }

    internal Label CreateLabel(KickAss.Label source)
        => new Label(
            FileLocationToTextRange(source.FileLocation),
            source.Name,
            source.Address);

    internal TextRange FileLocationToTextRange(KickAss.FileLocation source)
        => new TextRange(
            new TextCursor(source.Line1, source.Col1),
            new TextCursor(source.Line2, source.Col2));

    internal async ValueTask<ImmutableArray<SourceFile>> BuildSourceFiles(KickAss.C64Debugger dbgData, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    internal async ValueTask<SourceFile> BuildSourceFile(KickAss.Source source, KickAss.C64Debugger dbgData, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}