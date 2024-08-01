using Microsoft.Extensions.DependencyInjection;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

namespace Righthand.RetroDbgDataProvider;

public static class IoCRegistrar
{
    public static IServiceCollection AddDebugDataProvider(this IServiceCollection services)
    {
        return services
            .AddSingleton<IKickAssemblerCompiler, KickAssemblerCompiler>()
            .AddSingleton<IKickAssemblerByteDumpParser, KickAssemblerByteDumpParser>()
            .AddSingleton<IKickAssemblerDbgParser, KickAssemblerDbgParser>()
            .AddSingleton<IKickAssemblerProgramInfoBuilder, KickAssemblerProgramInfoBuilder>();

    }
}