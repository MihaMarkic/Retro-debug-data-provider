# Retro Debug Data Provider

This project goal is to provide as much debugging support as possible for retro assemblers and possibly compilers starting with [KickAssembler](https://www.theweb.dk/KickAssembler/Main.html#frontpage).

## Current services
* Debug file (.dbg) parsing provided by `IKickAssemblerDbgParser`.
* Byte dump file (.dmp) parsing provided by `IKickAssemblerByteDumpParser`.
* Compiler invocation (NOTE: Kick Assembler requires java installed on computer, OpenJDK is fine) provide by `IKickAssemblerCompiler`.
* Converting Kick Assembler specific data into universal model provided by `IKickAssemblerProgramInfoBuilder`.
* Universal model - for different assemblers and compilers (early version, might change in future) with top class `AssemblerAppInfo`.

I will add more services and more support types along the path.

This is a .NET 8+ cross-platform library.

## Dependency injection support

All service classes have matching interface. Register them manually or run `IoCRegistrar.AddDebugDataProvider` extension method on `IServiceCollection` instance like:

```csharp
public static IServiceCollection Configure(this IServiceCollection services)
{
    services.AddDebugDataProvider();
}
```

## Debug file parser

Get debug data model by calling 
```csharp
var model = await IKickAssemblerDbgParser.LoadFileAsync("PATH_TO_DBG_FILE", ct)
```

## Byte dump file parser

Get byte dumb model by calling 
```csharp
var model = await IKickAssemblerByteDumpParser.LoadFileAsync("PATH_TO_BYTE_DUMP_FILE", ct)
```

## Compiler invocation

## Unified model converter

Convert Kick Assembler specific debug model to universal one by calling 
```csharp
var universalModel = await IKickAssemblerProgramInfoBuilder.BuildAppInfoAsync("PROJECT_DIRECTORY", debugData, ct)
``` 
where `debugData` argument is output from [Debug file parser](#debug-file-parser). 